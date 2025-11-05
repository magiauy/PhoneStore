using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Text;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Text;

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
            _currentDialog = null;
            _isInitialized = false;
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
                await ShowProductDetailDialogAsync(product);
            }
        }

        private async Task ShowProductDetailDialogAsync(ProductListItemViewModel product)
        {
            try
            {
                var detail = ProductService.GetProductDetail(product.Id);
                if (detail == null)
                {
                    ShowMessageDialog(
                        _resourceLoader.GetString("ProductDetailErrorTitle"),
                        _resourceLoader.GetString("ProductNotFoundError"));
                    return;
                }

                var panel = new StackPanel { Spacing = 12 };

                panel.Children.Add(CreateDetailTextBlock(string.Format(_resourceLoader.GetString("ProductDetailSku"), detail.Product.Sku)));
                panel.Children.Add(CreateDetailTextBlock(string.Format(_resourceLoader.GetString("ProductDetailName"), detail.Product.Name)));
                panel.Children.Add(CreateDetailTextBlock(string.Format(_resourceLoader.GetString("ProductDetailPrice"), detail.Product.Price.ToString("C0", CultureInfo.CurrentCulture))));
                panel.Children.Add(CreateDetailTextBlock(string.Format(_resourceLoader.GetString("ProductDetailSerialCount"), detail.Product.SerialCount)));

                if (detail.AttributeValues.Count > 0)
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = _resourceLoader.GetString("ProductAttributesHeader"),
                        FontWeight = FontWeights.SemiBold,
                        FontSize = 14
                    });

                    var attributesPanel = new StackPanel { Spacing = 4 };
                    foreach (var attribute in detail.AttributeValues)
                    {
                        var value = string.IsNullOrWhiteSpace(attribute.DisplayValue)
                            ? _resourceLoader.GetString("ProductAttributeEmptyValue")
                            : attribute.DisplayValue;
                        attributesPanel.Children.Add(new TextBlock
                        {
                            Text = string.Format(_resourceLoader.GetString("ProductAttributeFormat"), attribute.AttributeName, value),
                            TextWrapping = TextWrapping.WrapWholeWords,
                            FontSize = 13
                        });
                    }

                    panel.Children.Add(attributesPanel);
                }
                else
                {
                    panel.Children.Add(new TextBlock
                    {
                        Text = _resourceLoader.GetString("ProductNoAttributes"),
                        FontStyle = FontStyle.Italic,
                        FontSize = 13
                    });
                }

                var dialog = new ContentDialog
                {
                    Title = string.Format(_resourceLoader.GetString("ProductDetailDialogTitle"), detail.Product.Name),
                    Content = panel,
                    CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                    XamlRoot = XamlRoot
                };

                _currentDialog = dialog;
                dialog.Closed += (_, _) => _currentDialog = null;
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                ShowMessageDialog(
                    _resourceLoader.GetString("ProductDetailErrorTitle"),
                    string.Format(_resourceLoader.GetString("ProductDetailErrorMessage"), ex.Message));
            }
        }

        private static TextBlock CreateDetailTextBlock(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 14,
                TextWrapping = TextWrapping.WrapWholeWords
            };
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
