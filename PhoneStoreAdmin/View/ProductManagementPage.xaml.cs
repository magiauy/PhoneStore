using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using System.Runtime.InteropServices.WindowsRuntime;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductManagementPage : Page
    {
        private readonly IProductService _productService;
        private readonly ResourceLoader _resourceLoader;
        private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;
        private readonly NotifyCollectionChangedEventHandler _detailCollectionChangedHandler;
        private ProductDialog? _currentDialog;
        private bool _isLoaded;
        private DetailSectionTab _activeDetailTab = DetailSectionTab.BatchHistory;

        public ObservableCollection<ProductListItemViewModel> Products { get; } = new();

        private ProductModelListItemViewModel? _currentModel;
        private ProductListItemViewModel? _selectedProduct;
        private ProductManagementDetailViewModel? _selectedProductDetail;
        private ProductBatchSummaryViewModel? _selectedBatch;
        private IncrementalCollection<ProductBatchSummaryViewModel>? _batchHistorySource;
        private IncrementalCollection<ProductSerialViewModel>? _productSerialSource;
        private IncrementalCollection<ProductSerialViewModel>? _batchSerialSource;

        public ProductModelListItemViewModel? CurrentModel
        {
            get => _currentModel;
            private set
            {
                _currentModel = value;
                Bindings.Update();
                EnsureActiveTabIsValid();
            }
        }

        public ProductListItemViewModel? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (_selectedProduct == value)
                {
                    return;
                }

                _selectedProduct = value;
                if (value != null)
                {
                    LoadProductDetail(value.Id);
                }
                else
                {
                    SelectedProductDetail = null;
                }

                Bindings.Update();
            }
        }

        public ProductManagementDetailViewModel? SelectedProductDetail
        {
            get => _selectedProductDetail;
            private set
            {
                if (_selectedProductDetail == value)
                {
                    return;
                }

                _selectedProductDetail = value;
                System.Diagnostics.Debug.WriteLine($"[DEBUG] SelectedProductDetail: Set to {(value != null ? value.Product.Name : "null")}, HasSelectedProduct will be {(value != null)}");
                
                if (value == null)
                {
                    SelectedBatch = null;
                    BatchHistorySource = null;
                    ProductSerialSource = null;
                    BatchSerialSource = null;
                }

                Bindings.Update();
            }
        }

        public bool HasProducts => Products.Count > 0;
        public bool NoProducts => !HasProducts;
        public bool HasSelectedProduct => SelectedProductDetail != null;
        public bool NoProductSelected => !HasSelectedProduct;
        public bool HasBatchHistory => SelectedProductDetail?.HasBatches ?? false;
        public bool NoBatchHistory => !HasBatchHistory;
        public bool HasSerials => SelectedProductDetail?.HasSerials ?? false;
        public bool NoSerials => !HasSerials;
        public bool IsBatchTabActive => _activeDetailTab == DetailSectionTab.BatchHistory;
        public bool IsSerialTabActive => _activeDetailTab == DetailSectionTab.SerialList;

        public IncrementalCollection<ProductBatchSummaryViewModel>? BatchHistorySource
        {
            get => _batchHistorySource;
            private set
            {
                if (_batchHistorySource == value)
                {
                    return;
                }

                UpdateCollectionSubscription(_batchHistorySource, value);
                _batchHistorySource = value;
                Bindings.Update();
            }
        }

        public IncrementalCollection<ProductSerialViewModel>? ProductSerialSource
        {
            get => _productSerialSource;
            private set
            {
                if (_productSerialSource == value)
                {
                    return;
                }

                UpdateCollectionSubscription(_productSerialSource, value);
                _productSerialSource = value;
                Bindings.Update();
            }
        }

        public IncrementalCollection<ProductSerialViewModel>? BatchSerialSource
        {
            get => _batchSerialSource;
            private set
            {
                if (_batchSerialSource == value)
                {
                    return;
                }

                UpdateCollectionSubscription(_batchSerialSource, value);
                _batchSerialSource = value;
                Bindings.Update();
            }
        }

        public ProductBatchSummaryViewModel? SelectedBatch
        {
            get => _selectedBatch;
            set
            {
                if (_selectedBatch == value)
                {
                    return;
                }

                _selectedBatch = value;
                BatchSerialSource?.Refresh();
                if (value != null)
                {
                    _ = BatchSerialSource?.PrimeAsync();
                }

                Bindings.Update();
            }
        }

        public bool HasSelectedBatch => SelectedBatch != null;
        public bool NoBatchSelected => !HasSelectedBatch;
        public bool HasBatchSerials => (BatchSerialSource?.Count ?? 0) > 0;
        public bool NoBatchSerials => HasSelectedBatch && !HasBatchSerials;

        public string CurrentModelName => CurrentModel?.Name ?? string.Empty;
        public string HeaderSubtitle
        {
            get
            {
                if (CurrentModel == null)
                {
                    return string.Empty;
                }

                var format = _resourceLoader.GetString("ProductManagementSubtitleFormat");
                if (string.IsNullOrWhiteSpace(format))
                {
                    format = "Variants belonging to {0}";
                }

                return string.Format(format, CurrentModel.Name);
            }
        }

        public string HeaderDescriptionText => string.IsNullOrWhiteSpace(HeaderSubtitle)
            ? _resourceLoader.GetString("ProductManagement_SubtitlePlaceholder") ?? "Subtitle"
            : HeaderSubtitle;

        public string SelectedProductSummary
        {
            get
            {
                if (SelectedProductDetail?.Product == null)
                {
                    return string.Empty;
                }

                var product = SelectedProductDetail.Product;
                return $"{product.Sku} • {product.CategoryName}";
            }
        }

        public string SelectedVariationInfo
        {
            get
            {
                var product = SelectedProductDetail?.Product;
                var placeholder = _resourceLoader.GetString("ProductManagement_VariationPlaceholder") ?? "128GB - Black";
                if (product == null)
                {
                    return placeholder;
                }

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(product.ModelName))
                {
                    parts.Add(product.ModelName);
                }

                if (!string.IsNullOrWhiteSpace(product.CategoryName))
                {
                    parts.Add(product.CategoryName);
                }

                if (!string.IsNullOrWhiteSpace(product.Sku))
                {
                    parts.Add(product.Sku);
                }

                return parts.Count == 0 ? placeholder : string.Join(" • ", parts);
            }
        }

        public string SelectedStatusText => SelectedProductDetail?.Product?.StatusText 
            ?? (_resourceLoader.GetString("ProductManagement_StatusPlaceholder") ?? "Active");

        public string SerialCountStat
        {
            get
            {
                var count = SelectedProductDetail?.SerialCount ?? 0;
                return count.ToString("N0");
            }
        }

        public string SelectedProductName => SelectedProductDetail?.Product?.Name ?? string.Empty;
        public string SelectedProductSku => SelectedProductDetail?.Product?.Sku ?? string.Empty;
        public string SelectedProductBrand => SelectedProductDetail?.Product?.BrandName ?? string.Empty;
        public IReadOnlyList<ProductBatchSummaryViewModel>? SelectedProductBatches => SelectedProductDetail?.Batches;
        public IReadOnlyList<ProductSerialViewModel>? SelectedProductSerials => SelectedProductDetail?.Serials;

        public string SerialHeader
        {
            get
            {
                var count = SelectedProductDetail?.SerialCount ?? 0;
                var format = _resourceLoader.GetString("ProductDetailSerialCount");
                if (string.IsNullOrWhiteSpace(format))
                {
                    var fallback = _resourceLoader.GetString("ProductManagementSerialsLabel.Text");
                    return string.IsNullOrWhiteSpace(fallback)
                        ? $"Serials: {count}"
                        : fallback;
                }

                return string.Format(format, count);
            }
        }

        public string SelectedBatchSerialHeader
        {
            get
            {
                if (SelectedBatch == null)
                {
                    return _resourceLoader.GetString("ProductManagementBatchSerialsTitle/Text") ?? string.Empty;
                }

                var format = _resourceLoader.GetString("ProductManagementBatchSerialHeaderFormat");
                if (string.IsNullOrWhiteSpace(format))
                {
                    format = "Serials in batch {0}";
                }

                return string.Format(format, SelectedBatch.BatchIdDisplay);
            }
        }

        public string TotalQuantityDisplay
        {
            get
            {
                var quantity = SelectedProductDetail?.TotalQuantity ?? 0;
                return quantity.ToString("N0");
            }
        }

        public ProductManagementPage()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _resourceLoader = new ResourceLoader();
            _collectionChangedHandler = (_, _) => Bindings.Update();
            _detailCollectionChangedHandler = (_, _) => Bindings.Update();
            Products.CollectionChanged += _collectionChangedHandler;
            Loaded += ProductManagementPage_Loaded;
            Unloaded += ProductManagementPage_Unloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is ProductModelDetailViewModel detail && detail.Model != null)
            {
                CurrentModel = detail.Model;
            }
            else if (e.Parameter is ProductModelListItemViewModel model)
            {
                CurrentModel = model;
            }

            if (_isLoaded)
            {
                LoadProducts();
            }
        }

        private void ProductManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            Products.CollectionChanged -= _collectionChangedHandler;
            Products.CollectionChanged += _collectionChangedHandler;
            _isLoaded = true;
            LoadProducts();
            UpdateDetailTabVisualState();
        }

        private void ProductManagementPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            Products.CollectionChanged -= _collectionChangedHandler;
            _currentDialog = null;
            BatchHistorySource = null;
            ProductSerialSource = null;
            BatchSerialSource = null;
            _selectedBatch = null;
        }

        private void LoadProducts(int? selectedProductId = null)
        {
            if (!_isLoaded || CurrentModel == null)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProducts: Early exit - _isLoaded={_isLoaded}, CurrentModel={CurrentModel?.Name ?? "null"}");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProducts: Loading products for model {CurrentModel.Id}");
                var items = _productService.GetProductsByModel(CurrentModel.Id) ?? Array.Empty<ProductListItemViewModel>();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProducts: Found {items.Count} products");
                Products.Clear();
                foreach (var item in items)
                {
                    Products.Add(item);
                }

                ProductListItemViewModel? selection = null;
                if (selectedProductId.HasValue)
                {
                    selection = Products.FirstOrDefault(p => p.Id == selectedProductId.Value);
                }

                selection ??= Products.FirstOrDefault();
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProducts: Setting SelectedProduct to {selection?.Name ?? "null"}");
                SelectedProduct = selection;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProducts: Exception - {ex.Message}");
                Logger.Error("Failed to load products for product management page", ex);
            }
        }

        private void LoadProductDetail(int productId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProductDetail: Loading detail for productId={productId}");
                var detail = _productService.GetProductManagementDetail(productId);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProductDetail: Detail loaded - {(detail != null ? "Success" : "Null")}");
                if (detail != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProductDetail: Product={detail.Product.Name}, Batches={detail.Batches.Count}, Serials={detail.Serials.Count}");
                }
                SelectedProductDetail = detail;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadProductDetail: Exception - {ex.Message}");
                Logger.Error($"Failed to load product detail for {productId}", ex);
                SelectedProductDetail = null;
            }
        }

        private async void CreateProductButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowProductDialogAsync(ProductDialog.DialogMode.Add);
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (!HasSelectedProduct)
            {
                return;
            }

            await ShowProductDialogAsync(ProductDialog.DialogMode.Edit);
        }

        private async Task ShowProductDialogAsync(ProductDialog.DialogMode mode)
        {
            if (_currentDialog != null)
            {
                return;
            }

            var dialog = new ProductDialog
            {
                XamlRoot = XamlRoot,
                Title = _resourceLoader.GetString(mode == ProductDialog.DialogMode.Add
                    ? "AddProductDialogTitle"
                    : "EditProductDialogTitle"),
                PrimaryButtonText = _resourceLoader.GetString("DialogSaveButton"),
                CloseButtonText = _resourceLoader.GetString("DialogCloseButton")
            };

            dialog.ProductSaved += ProductDialog_ProductSaved;

            ProductDetailViewModel? detail = null;
            if (mode == ProductDialog.DialogMode.Edit)
            {
                var productId = SelectedProductDetail?.Product.Id;
                if (!productId.HasValue)
                {
                    return;
                }

                detail = _productService.GetProductDetail(productId.Value);
                if (detail == null)
                {
                    Logger.Warning($"Product detail {productId.Value} not found for editing");
                    return;
                }
            }

            // Pass the current model context to the dialog if present
            if (CurrentModel != null)
            {
                dialog.SetContextModel(CurrentModel);
            }
            dialog.SetMode(mode, detail);

            if (mode == ProductDialog.DialogMode.Add && CurrentModel != null)
            {
                dialog.SetPreselectedModel(CurrentModel.Id);
            }

            dialog.Closed += (_, _) =>
            {
                dialog.ProductSaved -= ProductDialog_ProductSaved;
                _currentDialog = null;
            };

            _currentDialog = dialog;
            await dialog.ShowAsync();
        }

        private void SetActiveDetailTab(DetailSectionTab tab)
        {
            if (_activeDetailTab == tab)
            {
                UpdateDetailTabVisualState();
                return;
            }

            _activeDetailTab = tab;
            Bindings.Update();
            UpdateDetailTabVisualState();
        }

        private void EnsureActiveTabIsValid()
        {
            if (!HasSelectedProduct)
            {
                SetActiveDetailTab(DetailSectionTab.BatchHistory);
                return;
            }

            if (_activeDetailTab == DetailSectionTab.BatchHistory && !HasBatchHistory && HasSerials)
            {
                SetActiveDetailTab(DetailSectionTab.SerialList);
            }
            else if (_activeDetailTab == DetailSectionTab.SerialList && !HasSerials && HasBatchHistory)
            {
                SetActiveDetailTab(DetailSectionTab.BatchHistory);
            }
            else
            {
                UpdateDetailTabVisualState();
            }
        }

        private void UpdateDetailTabVisualState()
        {
            if (BatchTabButton == null || SerialTabButton == null)
            {
                return;
            }

            ApplyTabButtonState(BatchTabButton, _activeDetailTab == DetailSectionTab.BatchHistory, true);
            ApplyTabButtonState(SerialTabButton, _activeDetailTab == DetailSectionTab.SerialList, false);
        }

        private static void ApplyTabButtonState(Button button, bool isActive, bool isFirstTab)
        {
            button.IsEnabled = !isActive;
            button.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                isActive
                    ? Windows.UI.Color.FromArgb(255, 219, 234, 254)
                    : Windows.UI.Color.FromArgb(0, 0, 0, 0));
            button.CornerRadius = isFirstTab ? new CornerRadius(12, 0, 0, 12) : new CornerRadius(0, 12, 12, 0);

            if (button.Content is TextBlock textBlock)
            {
                textBlock.FontWeight = isActive
                    ? Microsoft.UI.Text.FontWeights.SemiBold
                    : Microsoft.UI.Text.FontWeights.Normal;
                textBlock.Foreground = isActive
                    ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushPrimary"]
                    : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["BrushTextSecondary"];
            }
        }

        private void BatchTabButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveDetailTab(DetailSectionTab.BatchHistory);
        }

        private void SerialTabButton_Click(object sender, RoutedEventArgs e)
        {
            SetActiveDetailTab(DetailSectionTab.SerialList);
        }

        private void ProductDialog_ProductSaved(object? sender, ProductDetailViewModel e)
        {
            LoadProducts(e.Product.Id);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current is App app && app.CurrentWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToPage("Products");
            }
        }

        private void InitializeIncrementalSources(int productId)
        {
            BatchHistorySource = new IncrementalCollection<ProductBatchSummaryViewModel>((page, pageSize) =>
                _productService.GetProductBatchHistory(productId, page, pageSize));

            ProductSerialSource = new IncrementalCollection<ProductSerialViewModel>((page, pageSize) =>
                _productService.GetProductSerials(productId, page, pageSize));

            BatchSerialSource = new IncrementalCollection<ProductSerialViewModel>((page, pageSize) =>
            {
                if (SelectedBatch == null)
                {
                    return PagedResult<ProductSerialViewModel>.CreateEmpty();
                }

                return _productService.GetProductSerials(productId, page, pageSize, SelectedBatch.BatchId);
            });

            SelectedBatch = null;

            _ = BatchHistorySource?.PrimeAsync();
            _ = ProductSerialSource?.PrimeAsync();
        }

        private void UpdateCollectionSubscription(INotifyCollectionChanged? oldValue, INotifyCollectionChanged? newValue)
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= _detailCollectionChangedHandler;
            }

            if (newValue != null)
            {
                newValue.CollectionChanged += _detailCollectionChangedHandler;
            }
        }

        public sealed class IncrementalCollection<T> : ObservableCollection<T>, ISupportIncrementalLoading
        {
            private readonly Func<int, int, PagedResult<T>> _loader;
            private readonly int _pageSize;
            private int _currentPage;
            private bool _isLoading;

            public bool HasMoreItems { get; private set; } = true;

            public IncrementalCollection(Func<int, int, PagedResult<T>> loader, int pageSize = 20)
            {
                _loader = loader ?? throw new ArgumentNullException(nameof(loader));
                _pageSize = pageSize <= 0 ? 20 : pageSize;
            }

            public void Refresh()
            {
                Clear();
                _currentPage = 0;
                HasMoreItems = true;
            }

            public Task PrimeAsync()
            {
                if (_isLoading || !HasMoreItems || Count > 0)
                {
                    return Task.CompletedTask;
                }

                return LoadMoreItemsAsync((uint)_pageSize).AsTask();
            }

            public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
            {
                return AsyncInfo.Run(async cancellationToken =>
                {
                    if (_isLoading || !HasMoreItems)
                    {
                        return new LoadMoreItemsResult { Count = 0 };
                    }

                    _isLoading = true;
                    try
                    {
                        var nextPage = _currentPage + 1;
                        var result = await Task.Run(() => _loader(nextPage, _pageSize), cancellationToken);
                        var items = (result?.Items ?? Array.Empty<T>()).ToList();
                        foreach (var item in items)
                        {
                            Add(item);
                        }

                        _currentPage = nextPage;
                        var totalRecords = result?.TotalRecords ?? Count;
                        HasMoreItems = Count < totalRecords;
                        return new LoadMoreItemsResult { Count = (uint)items.Count };
                    }
                    finally
                    {
                        _isLoading = false;
                    }
                });
            }
        }

        private enum DetailSectionTab
        {
            BatchHistory,
            SerialList
        }

    }
}
