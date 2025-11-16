using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductManagementPage : Page
    {
        private readonly IProductService _productService;
        private readonly ResourceLoader _resourceLoader;
        private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;

        private bool _isLoaded;
        private ProductListItemViewModel? _selectedProduct;
        private ProductManagementDetailViewModel? _selectedProductDetail;
        private ContentDialog? _activeDialog;

        public ObservableCollection<ProductListItemViewModel> Products { get; } = new();

        public string ProductCountLabel => $"({Products.Count})";

        public ProductListItemViewModel? SelectedProduct
        {
            get => _selectedProduct;
            set
            {
                if (!Equals(_selectedProduct, value))
                {
                    _selectedProduct = value;
                    Bindings.Update();

                    if (_isLoaded && value != null)
                    {
                        LoadProductDetail(value.Id);
                    }
                    else if (value == null)
                    {
                        SelectedProductDetail = null;
                    }
                }
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

        public bool HasSelectedProductDetail => SelectedProductDetail != null;
        public bool HasNoSelectedProductDetail => !HasSelectedProductDetail;

        public string SelectedProductName => SelectedProductDetail?.Product.Name ?? string.Empty;
        public string SelectedProductSku => SelectedProductDetail?.Product.Sku ?? string.Empty;
        public string SelectedProductStatusText => SelectedProductDetail?.Product.StatusText ?? string.Empty;

        public int SelectedProductTotalQuantity => SelectedProductDetail?.TotalQuantity ?? 0;
        public decimal SelectedProductAverageCostPrice => SelectedProductDetail?.AverageCostPrice ?? 0m;
        public decimal SelectedProductAverageSellingPrice => SelectedProductDetail?.AverageSellingPrice ?? 0m;

        public IReadOnlyList<ProductBatchDetailViewModel> SelectedProductBatches => SelectedProductDetail?.Batches ?? Array.Empty<ProductBatchDetailViewModel>();
        public bool HasSelectedProductBatches => SelectedProductDetail?.HasBatches ?? false;
        public bool HasNoSelectedProductBatches => !HasSelectedProductBatches;

        public bool HasProducts => Products.Count > 0;
        public bool HasNoProducts => !HasProducts;

        public ProductManagementPage()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _resourceLoader = new ResourceLoader();
            _collectionChangedHandler = (_, __) => Bindings.Update();

            Loaded += ProductManagementPage_Loaded;
            Unloaded += ProductManagementPage_Unloaded;
            Products.CollectionChanged += _collectionChangedHandler;
        }

        private void ProductManagementPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = true;
            Products.CollectionChanged -= _collectionChangedHandler;
            Products.CollectionChanged += _collectionChangedHandler;
            LoadProducts();
        }

        private void ProductManagementPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            _activeDialog = null;
            Products.CollectionChanged -= _collectionChangedHandler;
        }

        private void LoadProducts(int? selectedProductId = null)
        {
            if (!_isLoaded)
            {
                return;
            }

            Products.CollectionChanged -= _collectionChangedHandler;
            try
            {
                var catalog = _productService.GetProductCatalog();

                Products.Clear();
                foreach (var product in catalog)
                {
                    Products.Add(product);
                }

                Bindings.Update();

                ProductListItemViewModel? target = null;
                if (selectedProductId.HasValue)
                {
                    target = Products.FirstOrDefault(p => p.Id == selectedProductId.Value);
                }

                target ??= Products.FirstOrDefault();
                SelectedProduct = target;

                if (target == null)
                {
                    SelectedProductDetail = null;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load product catalog: {ex.Message}", ex);
            }
            finally
            {
                Products.CollectionChanged += _collectionChangedHandler;
            }
        }

        private void LoadProductDetail(int productId)
        {
            try
            {
                SelectedProductDetail = _productService.GetProductManagementDetail(productId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load product detail: {ex.Message}", ex);
                SelectedProductDetail = null;
            }
        }

        private void ProductGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProductListItemViewModel product)
            {
                SelectedProduct = product;
            }
        }

        private void RefreshProductsButton_Click(object sender, RoutedEventArgs e)
        {
            var currentId = SelectedProduct?.Id;
            LoadProducts(currentId);
        }

        private async void AddProductButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowProductDialogAsync(ProductDialog.DialogMode.Add, null);
        }

        private async void EditProductButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedProduct == null)
            {
                return;
            }

            var detail = _productService.GetProductDetail(SelectedProduct.Id);
            if (detail == null)
            {
                return;
            }

            await ShowProductDialogAsync(ProductDialog.DialogMode.Edit, detail);
        }

        private async Task ShowProductDialogAsync(ProductDialog.DialogMode mode, ProductDetailViewModel? detail)
        {
            try
            {
                var dialogContent = new ProductDialog();
                dialogContent.SetMode(mode, detail);
                dialogContent.ProductSaved += ProductDialog_ProductSaved;

                var dialog = new ContentDialog
                {
                    Title = GetDialogTitle(mode, detail?.Product.Name),
                    PrimaryButtonText = _resourceLoader.GetString("DialogSaveButton"),
                    CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                    XamlRoot = XamlRoot,
                    DefaultButton = ContentDialogButton.Primary,
                    Content = dialogContent
                };

                dialog.PrimaryButtonClick += (_, args) =>
                {
                    if (!dialogContent.Save())
                    {
                        args.Cancel = true;
                    }
                };

                dialog.Closed += (_, __) =>
                {
                    dialogContent.ProductSaved -= ProductDialog_ProductSaved;
                };

                _activeDialog = dialog;
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to show product dialog: {ex.Message}", ex);
            }
            finally
            {
                _activeDialog = null;
            }
        }

        private void ProductDialog_ProductSaved(object? sender, ProductDetailViewModel e)
        {
            LoadProducts(e.Product.Id);
        }

        private string GetDialogTitle(ProductDialog.DialogMode mode, string? productName)
        {
            return mode switch
            {
                ProductDialog.DialogMode.Edit => string.Format(_resourceLoader.GetString("EditProductDialogTitle"), productName ?? string.Empty),
                _ => _resourceLoader.GetString("AddProductDialogTitle")
            };
        }
    }
}
