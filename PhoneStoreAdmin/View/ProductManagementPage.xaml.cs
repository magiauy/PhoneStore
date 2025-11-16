using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductManagementPage : Page
    {
        private readonly IProductService _productService;
        private readonly ResourceLoader _resourceLoader;
        private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;
        private ContentDialog? _currentDialog;
        private bool _isLoaded;

        public ObservableCollection<ProductListItemViewModel> Products { get; } = new();

        private ProductModelListItemViewModel? _currentModel;
        private ProductListItemViewModel? _selectedProduct;
        private ProductManagementDetailViewModel? _selectedProductDetail;

        public ProductModelListItemViewModel? CurrentModel
        {
            get => _currentModel;
            private set
            {
                _currentModel = value;
                Bindings.Update();
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
                _selectedProductDetail = value;
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
        }

        private void ProductManagementPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            Products.CollectionChanged -= _collectionChangedHandler;
            _currentDialog = null;
        }

        private void LoadProducts(int? selectedProductId = null)
        {
            if (!_isLoaded || CurrentModel == null)
            {
                return;
            }

            try
            {
                var items = _productService.GetProductsByModel(CurrentModel.Id) ?? Array.Empty<ProductListItemViewModel>();
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
                SelectedProduct = selection;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load products for product management page", ex);
            }
        }

        private void LoadProductDetail(int productId)
        {
            try
            {
                var detail = _productService.GetProductManagementDetail(productId);
                SelectedProductDetail = detail;
            }
            catch (Exception ex)
            {
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

            var dialogContent = new ProductDialog();
            dialogContent.ProductSaved += ProductDialog_ProductSaved;

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

            dialogContent.SetMode(mode, detail);
            if (mode == ProductDialog.DialogMode.Add && CurrentModel != null)
            {
                dialogContent.SetPreselectedModel(CurrentModel.Id);
            }

            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString(mode == ProductDialog.DialogMode.Add
                    ? "AddProductDialogTitle"
                    : "EditProductDialogTitle"),
                PrimaryButtonText = _resourceLoader.GetString("DialogSaveButton"),
                CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                Content = dialogContent,
                XamlRoot = XamlRoot
            };

            dialog.PrimaryButtonClick += (_, args) =>
            {
                if (!dialogContent.Save())
                {
                    args.Cancel = true;
                }
            };

            dialog.Closed += (_, _) =>
            {
                dialogContent.Cancel();
                dialogContent.ProductSaved -= ProductDialog_ProductSaved;
                _currentDialog = null;
            };

            _currentDialog = dialog;
            await dialog.ShowAsync();
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

        public string FormatBatchId(int batchId)
        {
            var format = _resourceLoader.GetString("Common_BatchIdFormat");
            if (string.IsNullOrWhiteSpace(format))
            {
                format = "#{0}";
            }

            return string.Format(format, batchId);
        }
    }
}
