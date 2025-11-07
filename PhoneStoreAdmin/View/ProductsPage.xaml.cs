using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductsPage : Page
    {
        public sealed class SelectionOption<T>
        {
            public SelectionOption(string displayName, T value)
            {
                DisplayName = displayName;
                Value = value;
            }

            public string DisplayName { get; }
            public T Value { get; }
        }

        private IProductService ProductService => App.GetService<IProductService>();
        private IBrandService BrandService => App.GetService<IBrandService>();
        private IProductCategoryRepository CategoryRepository => App.GetService<IProductCategoryRepository>();

        private readonly ResourceLoader _resourceLoader;
        private bool _isInitialized;
        private bool _isUpdatingFilters;
        private ContentDialog? _currentDialog;
        private ProductDialog? _productDialog;
        private ContentDialog? _productDialogHost;
        private ProductListItemViewModel? _selectedProduct;
        private int? _pendingSelectionProductId;

        public ObservableCollection<ProductListItemViewModel> Products { get; } = new();
        public ObservableCollection<SelectionOption<int?>> BrandOptions { get; } = new();
        public ObservableCollection<SelectionOption<int?>> CategoryOptions { get; } = new();
        public ObservableCollection<SelectionOption<ProductStatus?>> StatusOptions { get; } = new();

        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        public ProductsPage()
        {
            InitializeComponent();
            _resourceLoader = new ResourceLoader();
            Loaded += ProductsPage_Loaded;
            Unloaded += ProductsPage_Unloaded;
        }

        private void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            InitializeFilterOptions();
            LoadProducts();
        }

        private void ProductsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            CloseProductDialog();
            DisposeProductDialog();
            _currentDialog = null;
            _isInitialized = false;
        }

        private void CloseProductDialog()
        {
            if (_productDialogHost != null)
            {
                try
                {
                    _productDialogHost.Hide();
                }
                catch
                {
                    // Ignore errors when dialog is already dismissed
                }
            }
        }

        private void DisposeProductDialog()
        {
            if (_productDialog != null)
            {
                _productDialog.ProductSaved -= ProductDialog_ProductSaved;
                _productDialog.DialogClosed -= ProductDialog_DialogClosed;
                _productDialog.ViewSerialsRequested -= ProductDialog_ViewSerialsRequested;
                _productDialog = null;
            }

            if (_productDialogHost != null)
            {
                _productDialogHost.PrimaryButtonClick -= ProductDialogHost_PrimaryButtonClick;
                _productDialogHost.CloseButtonClick -= ProductDialogHost_CloseButtonClick;
                _productDialogHost.Closed -= ProductDialogHost_Closed;
                _productDialogHost = null;
            }
        }

        private void InitializeFilterOptions()
        {
            try
            {
                _isUpdatingFilters = true;
                var allLabel = _resourceLoader.GetString("FilterOptionAll");

                if (BrandOptions.Count == 0)
                {
                    BrandOptions.Clear();
                    BrandOptions.Add(new SelectionOption<int?>(allLabel, null));

                    var brands = BrandService.GetAll() ?? Enumerable.Empty<Brand>();
                    foreach (var brand in brands.OrderBy(b => b.Name))
                    {
                        var displayName = string.IsNullOrWhiteSpace(brand.Name) ? _resourceLoader.GetString("UnknownBrandLabel") : brand.Name!;
                        BrandOptions.Add(new SelectionOption<int?>(displayName, brand.Id));
                    }

                    if (BrandFilterCombo != null)
                    {
                        BrandFilterCombo.SelectedIndex = 0;
                    }
                }

                if (CategoryOptions.Count == 0)
                {
                    CategoryOptions.Clear();
                    CategoryOptions.Add(new SelectionOption<int?>(allLabel, null));

                    var categories = CategoryRepository.GetAll() ?? Enumerable.Empty<ProductCategory>();
                    foreach (var category in categories.OrderBy(c => c.Name))
                    {
                        var displayName = string.IsNullOrWhiteSpace(category.Name) ? _resourceLoader.GetString("UnknownCategoryLabel") : category.Name;
                        CategoryOptions.Add(new SelectionOption<int?>(displayName, category.Id));
                    }

                    if (CategoryFilterCombo != null)
                    {
                        CategoryFilterCombo.SelectedIndex = 0;
                    }
                }

                if (StatusOptions.Count == 0)
                {
                    StatusOptions.Clear();
                    StatusOptions.Add(new SelectionOption<ProductStatus?>(allLabel, null));

                    foreach (var status in Enum.GetValues(typeof(ProductStatus)).Cast<ProductStatus>())
                    {
                        var key = status switch
                        {
                            ProductStatus.ACTIVE => "Status_Active",
                            ProductStatus.INACTIVE => "Status_Inactive",
                            ProductStatus.DISCONTINUED => "Product_Status_Discontinued",
                            _ => status.ToString()
                        };
                        var label = _resourceLoader.GetString(key);
                        if (string.IsNullOrWhiteSpace(label))
                        {
                            label = status.ToString();
                        }

                        StatusOptions.Add(new SelectionOption<ProductStatus?>(label, status));
                    }

                    if (StatusFilterCombo != null)
                    {
                        StatusFilterCombo.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("LoadProductsErrorTitle"),
                    string.Format(_resourceLoader.GetString("LoadProductsErrorMessage"), ex.Message));
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        private void LoadProducts()
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            try
            {
                if (ProductsListView != null)
                {
                    ProductsListView.SelectedItem = null;
                }

                UpdateSelectedProduct(null);
                Products.Clear();

                var criteria = BuildCurrentFilter();
                var result = ProductService.SearchProducts(criteria);
                if (result == null)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("LoadProductsErrorTitle"),
                        _resourceLoader.GetString("LoadProductsUnavailableMessage"));
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                if (TotalPages > 0 && CurrentPage > TotalPages)
                {
                    CurrentPage = TotalPages;
                    LoadProducts();
                    return;
                }

                if (TotalPages == 0)
                {
                    CurrentPage = 1;
                }

                foreach (var product in result.Products)
                {
                    Products.Add(product);
                }

                if (_pendingSelectionProductId.HasValue)
                {
                    var pendingProduct = Products.FirstOrDefault(p => p.Id == _pendingSelectionProductId.Value);
                    if (pendingProduct != null && ProductsListView != null)
                    {
                        ProductsListView.SelectedItem = pendingProduct;
                        UpdateSelectedProduct(pendingProduct);
                    }

                    _pendingSelectionProductId = null;
                }

                UpdatePaginationControls();
                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("LoadProductsErrorTitle"),
                    string.Format(_resourceLoader.GetString("LoadProductsErrorMessage"), ex.Message));
            }
        }

        private ProductFilterCriteria BuildCurrentFilter()
        {
            var brandOption = BrandFilterCombo?.SelectedItem as SelectionOption<int?>;
            var categoryOption = CategoryFilterCombo?.SelectedItem as SelectionOption<int?>;
            var statusOption = StatusFilterCombo?.SelectedItem as SelectionOption<ProductStatus?>;

            return new ProductFilterCriteria
            {
                Sku = SearchSkuBox?.Text,
                Name = SearchNameBox?.Text,
                BrandId = brandOption?.Value,
                CategoryId = categoryOption?.Value,
                Status = statusOption?.Value,
                Page = CurrentPage,
                PageSize = PageSize
            };
        }

        private void UpdatePaginationControls()
        {
            if (PageInfoText != null)
            {
                PageInfoText.Text = TotalPages <= 0 ? "0 / 0" : $"{CurrentPage} / {TotalPages}";
            }

            if (RecordCountText != null)
            {
                RecordCountText.Text = $"{Products.Count} / {TotalRecords}";
            }

            if (PreviousPageButton != null)
            {
                PreviousPageButton.IsEnabled = CurrentPage > 1;
            }

            if (NextPageButton != null)
            {
                NextPageButton.IsEnabled = TotalPages > 0 && CurrentPage < TotalPages;
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = Products.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            if (ProductsListView != null)
            {
                ProductsListView.Visibility = Products.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void SearchSkuBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                CurrentPage = 1;
                LoadProducts();
            }
        }

        private void SearchNameBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                CurrentPage = 1;
                LoadProducts();
            }
        }

        private void BrandFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            CurrentPage = 1;
            LoadProducts();
        }

        private void CategoryFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            CurrentPage = 1;
            LoadProducts();
        }

        private void StatusFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized || _isUpdatingFilters)
            {
                return;
            }

            CurrentPage = 1;
            LoadProducts();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _isUpdatingFilters = true;

                if (SearchSkuBox != null)
                {
                    SearchSkuBox.Text = string.Empty;
                }

                if (SearchNameBox != null)
                {
                    SearchNameBox.Text = string.Empty;
                }

                if (BrandFilterCombo != null)
                {
                    BrandFilterCombo.SelectedIndex = 0;
                }

                if (CategoryFilterCombo != null)
                {
                    CategoryFilterCombo.SelectedIndex = 0;
                }

                if (StatusFilterCombo != null)
                {
                    StatusFilterCombo.SelectedIndex = 0;
                }
            }
            finally
            {
                _isUpdatingFilters = false;
                CurrentPage = 1;
                LoadProducts();
            }
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadProducts();
            }
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {
            if (TotalPages > 0 && CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadProducts();
            }
        }

        private async void ProductItem_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ProductListItemViewModel product)
            {
                ProductsListView.SelectedItem = product;
                UpdateSelectedProduct(product);
                await OpenProductDialogAsync(ProductDialog.DialogMode.Edit, product);
            }
        }

        private async Task OpenProductDialogAsync(ProductDialog.DialogMode mode, ProductListItemViewModel? product = null)
        {
            try
            {
                ProductDetailViewModel? detail = null;

                if (mode != ProductDialog.DialogMode.Add)
                {
                    if (product == null)
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle"),
                            _resourceLoader.GetString("ProductNotFoundError"));
                        return;
                    }

                    try
                    {
                        detail = ProductService.GetProductDetail(product.Id);
                        if (detail == null)
                        {
                            // GetProductDetail returns null when there's an error
                            // The error has already been logged in the service layer
                            ShowMessageDialog(
                                _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Error Loading Product",
                                "Failed to load product details. Please check the application logs for more information.");
                            return;
                        }
                    }
                    catch (ArgumentException argEx)
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Invalid Input",
                            $"Invalid product information: {argEx.Message}");
                        return;
                    }
                    catch (InvalidOperationException opEx)
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Product Error",
                            $"Problem accessing product: {opEx.Message}");
                        return;
                    }
                    catch (Exception ex)
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Unexpected Error",
                            $"An unexpected error occurred while loading product details: {ex.Message}");
                        return;
                    }
                }

                CloseProductDialog();
                DisposeProductDialog();

                var dialogContent = new ProductDialog();
                dialogContent.ProductSaved += ProductDialog_ProductSaved;
                dialogContent.DialogClosed += ProductDialog_DialogClosed;
                dialogContent.ViewSerialsRequested += ProductDialog_ViewSerialsRequested;
                dialogContent.SetMode(mode, detail);

                var title = mode switch
                {
                    ProductDialog.DialogMode.Add => _resourceLoader.GetString("AddProductDialogTitle"),
                    ProductDialog.DialogMode.Edit => _resourceLoader.GetString("EditProductDialogTitle"),
                    ProductDialog.DialogMode.View => _resourceLoader.GetString("ProductDetailDialogTitle"),
                    _ => string.Empty
                };

                if (string.IsNullOrWhiteSpace(title))
                {
                    title = mode switch
                    {
                        ProductDialog.DialogMode.Add => "Add product",
                        ProductDialog.DialogMode.Edit => "Edit product",
                        _ => "Product details"
                    };
                }

                var primaryText = mode == ProductDialog.DialogMode.View
                    ? _resourceLoader.GetString("DialogCloseButton")
                    : _resourceLoader.GetString("DialogSave");
                if (string.IsNullOrWhiteSpace(primaryText))
                {
                    primaryText = mode == ProductDialog.DialogMode.View ? "Close" : "Save";
                }

                string? closeText = null;
                if (mode != ProductDialog.DialogMode.View)
                {
                    closeText = _resourceLoader.GetString("DialogCancel");
                    if (string.IsNullOrWhiteSpace(closeText))
                    {
                        closeText = "Cancel";
                    }
                }

                var dialog = new ContentDialog
                {
                    Title = title,
                    Content = dialogContent,
                    PrimaryButtonText = primaryText,
                    CloseButtonText = closeText,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                dialog.PrimaryButtonClick += ProductDialogHost_PrimaryButtonClick;
                dialog.CloseButtonClick += ProductDialogHost_CloseButtonClick;
                dialog.Closed += ProductDialogHost_Closed;

                _productDialog = dialogContent;
                _productDialogHost = dialog;

                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle"),
                    string.Format(_resourceLoader.GetString("ProductDetailErrorMessage"), ex.Message));
            }
        }

        private void ProductDialogHost_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_productDialog == null)
            {
                return;
            }

            args.Cancel = true;
            _productDialog.Save();
        }

        private void ProductDialogHost_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (_productDialog == null)
            {
                return;
            }

            args.Cancel = true;
            _productDialog.Cancel();
        }

        private void ProductDialogHost_Closed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            DisposeProductDialog();
        }

        private void ProductDialog_ProductSaved(object? sender, ProductDetailViewModel detail)
        {
            _pendingSelectionProductId = detail.Product.Id;
            LoadProducts();
        }

        private async void ProductDialog_ViewSerialsRequested(object? sender, int productId)
        {
            CloseProductDialog();

            try
            {
                // Validate product ID
                if (productId <= 0)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Invalid Product",
                        "Invalid product ID. Cannot load serial information.");
                    return;
                }

                // Get product detail with error handling
                ProductDetailViewModel? detail = null;
                try
                {
                    detail = ProductService.GetProductDetail(productId);
                }
                catch (Exception ex)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Error",
                        $"Error retrieving product details: {ex.Message}");
                    return;
                }

                // Check if product detail is null
                if (detail == null)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Product Not Found",
                        $"Could not load product {productId}. The product may have been deleted or there is a database error.");
                    return;
                }

                // Check if serials collection is empty
                if (detail.Serials == null || detail.Serials.Count == 0)
                {
                    var summary = _resourceLoader.GetString("ProductSerialSummaryFormat");
                    if (!string.IsNullOrWhiteSpace(summary))
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle") ?? "No Serials",
                            string.Format(summary, 0));
                    }
                    else
                    {
                        ShowMessageDialog(
                            _resourceLoader.GetString("ProductDetailErrorTitle") ?? "No Serials",
                            "This product has no serial numbers currently in stock.");
                    }
                    return;
                }

                // Process and display serials
                var serialPanel = new StackPanel { Spacing = 4 };
                int processedCount = 0;

                foreach (var serial in detail.Serials)
                {
                    // Validate serial object
                    if (serial == null)
                    {
                        continue;
                    }

                    // Extract serial display text with priority: SerialNumber > Imei1 > Imei2
                    var serialText = string.IsNullOrWhiteSpace(serial.SerialNumber)
                        ? string.IsNullOrWhiteSpace(serial.Imei1)
                            ? serial.Imei2 ?? string.Empty
                            : serial.Imei1!
                        : serial.SerialNumber!;

                    // Skip empty serials
                    if (string.IsNullOrWhiteSpace(serialText))
                    {
                        continue;
                    }

                    serialPanel.Children.Add(new TextBlock
                    {
                        Text = serialText,
                        FontSize = 14,
                        TextWrapping = TextWrapping.WrapWholeWords
                    });

                    processedCount++;
                }

                // Check if any valid serials were found
                if (serialPanel.Children.Count == 0)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("ProductDetailErrorTitle") ?? "No Serials",
                        $"Product has {detail.Serials.Count} serial record(s) but none contain valid serial number or IMEI information.");
                    return;
                }

                var scrollViewer = new ScrollViewer
                {
                    Content = serialPanel,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    MaxHeight = 320,
                    Padding = new Thickness(8)
                };

                var summaryText = _resourceLoader.GetString("ProductSerialSummaryFormat");
                if (string.IsNullOrWhiteSpace(summaryText))
                {
                    summaryText = "Serials available in stock: {0}";
                }

                var serialDialog = new ContentDialog
                {
                    Title = string.Format(summaryText, detail.Serials.Count),
                    Content = scrollViewer,
                    CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                    XamlRoot = XamlRoot
                };

                if (string.IsNullOrWhiteSpace(serialDialog.CloseButtonText))
                {
                    serialDialog.CloseButtonText = "Close";
                }

                _currentDialog = serialDialog;
                serialDialog.Closed += (_, _) => _currentDialog = null;
                await serialDialog.ShowAsync();
            }
            catch (ArgumentException argEx)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Invalid Input",
                    $"Invalid product ID: {argEx.Message}");
            }
            catch (InvalidOperationException opEx)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Database Error",
                    $"Error accessing product data: {opEx.Message}");
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle") ?? "Unexpected Error",
                    $"An unexpected error occurred while retrieving serial information: {ex.Message}");
            }
        }

        private void ProductDialog_DialogClosed(object? sender, EventArgs e)
        {
            CloseProductDialog();
        }

        private void UpdateSelectedProduct(ProductListItemViewModel? product)
        {
            _selectedProduct = product;

            if (EditProductButton != null)
            {
                EditProductButton.IsEnabled = _selectedProduct != null;
            }
        }

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            await OpenProductDialogAsync(ProductDialog.DialogMode.Add);
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle"),
                    _resourceLoader.GetString("ProductNotFoundError"));
                return;
            }

            await OpenProductDialogAsync(ProductDialog.DialogMode.Edit, _selectedProduct);
        }

        private void ProductsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProductsListView?.SelectedItem is ProductListItemViewModel selected)
            {
                UpdateSelectedProduct(selected);
            }
            else
            {
                UpdateSelectedProduct(null);
            }
        }

        private async void ShowMessageDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = string.IsNullOrWhiteSpace(title) ? _resourceLoader.GetString("GenericDialogTitle") : title,
                Content = message,
                CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                XamlRoot = XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}
