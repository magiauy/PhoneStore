using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services.Interfaces;
using PhoneStoreAdmin.View.Controls;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ProductsPage : Page
    {
        private const string ProductManagementPageTag = "ProductManagement";

        private readonly IProductService _productService;
        private readonly ResourceLoader _resourceLoader;
        private ContentDialog? _currentDialog;
        private bool _isLoaded;
        private ProductModelDetailViewModel? _selectedModelDetail;
        private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;

        public ObservableCollection<ProductModelListItemViewModel> ProductModels { get; } = new();

        public bool HasNoModels => ProductModels.Count == 0;

        public ProductModelDetailViewModel? SelectedModelDetail
        {
            get => _selectedModelDetail;
            private set
            {
                _selectedModelDetail = value;
                Bindings.Update();
            }
        }

        public string SelectedModelName => SelectedModelDetail?.Model.Name ?? string.Empty;
        
        public string SelectedModelDescription => SelectedModelDetail?.Model.Description ?? string.Empty;
        
        public IReadOnlyList<ProductListItemViewModel>? SelectedModelVariants => SelectedModelDetail?.Variants;
        
        public bool HasSelectedModel => SelectedModelDetail != null;
        
        public bool SelectedModelHasNoVariants => SelectedModelDetail != null && !SelectedModelDetail.HasVariants;

        public string SelectedModelSummary
        {
            get
            {
                if (SelectedModelDetail?.Model == null)
                {
                    return string.Empty;
                }

                var model = SelectedModelDetail.Model;
                return string.Format("{0} variants • Updated {1:g}", model.VariantCount, model.UpdatedAt.ToLocalTime());
            }
        }

        public ProductsPage()
        {
            InitializeComponent();
            _productService = App.GetService<IProductService>();
            _resourceLoader = new ResourceLoader();
            _collectionChangedHandler = (_, _) => Bindings.Update();
            Loaded += ProductsPage_Loaded;
            Unloaded += ProductsPage_Unloaded;
            ProductModels.CollectionChanged += _collectionChangedHandler;
        }

        private void ProductsPage_Loaded(object sender, RoutedEventArgs e)
        {
            ProductModels.CollectionChanged -= _collectionChangedHandler;
            ProductModels.CollectionChanged += _collectionChangedHandler;
            _isLoaded = true;
            LoadModels();
        }

        private void ProductsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            _currentDialog = null;
            ProductModels.CollectionChanged -= _collectionChangedHandler;
        }

        private void LoadModels(int? selectedModelId = null)
        {
            if (!_isLoaded)
            {
                return;
            }

            var summaries = _productService.GetProductModelSummaries();
            ProductModels.Clear();
            foreach (var model in summaries)
            {
                ProductModels.Add(model);
            }

            ProductModelListItemViewModel? selected = null;
            if (selectedModelId.HasValue)
            {
                selected = ProductModels.FirstOrDefault(m => m.Id == selectedModelId.Value);
            }

            if (selected == null)
            {
                selected = ProductModels.FirstOrDefault();
            }

            if (selected != null)
            {
                LoadModelDetail(selected.Id);
            }
            else
            {
                SelectedModelDetail = null;
            }
        }

        private void LoadModelDetail(int modelId)
        {
            var detail = _productService.GetProductModelDetail(modelId);
            SelectedModelDetail = detail;
        }

        private void RefreshModelsButton_Click(object sender, RoutedEventArgs e)
        {
            var currentId = SelectedModelDetail?.Model.Id;
            LoadModels(currentId);
        }

        private void ModelGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ProductModelListItemViewModel model)
            {
                LoadModelDetail(model.Id);
            }
        }

        private async void CreateModelButton_Click(object sender, RoutedEventArgs e)
        {
            await ShowProductModelDialogAsync(ProductModelDialog.DialogMode.Create, null);
        }

        private async void EditModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModelDetail?.Model == null)
            {
                return;
            }

            await ShowProductModelDialogAsync(ProductModelDialog.DialogMode.Edit, SelectedModelDetail.Model);
        }

        private async void DuplicateModelButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModelDetail?.Model == null)
            {
                return;
            }

            await ShowProductModelDialogAsync(ProductModelDialog.DialogMode.Duplicate, SelectedModelDetail.Model);
        }

        private async Task ShowProductModelDialogAsync(ProductModelDialog.DialogMode mode, ProductModelListItemViewModel? model)
        {
            try
            {
                var dialog = new ProductModelDialog
                {
                    Title = GetModelDialogTitle(mode, model?.Name),
                    PrimaryButtonText = _resourceLoader.GetString("DialogSaveButton"),
                    CloseButtonText = _resourceLoader.GetString("DialogCloseButton"),
                    XamlRoot = XamlRoot
                };
                
                dialog.SetMode(mode, model);
                dialog.ModelSaved += ProductModelDialog_ModelSaved;

                dialog.PrimaryButtonClick += (_, args) =>
                {
                    if (!dialog.Save())
                    {
                        args.Cancel = true;
                    }
                };

                dialog.Closed += (_, _) => dialog.Cancel();

                _currentDialog = dialog;
                await dialog.ShowAsync();
                _currentDialog = null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error opening product model dialog: {ex.Message}", ex);
            }
        }

        private void ProductModelDialog_ModelSaved(object? sender, ProductModelListItemViewModel e)
        {
            LoadModels(e.Id);
        }

        private string GetModelDialogTitle(ProductModelDialog.DialogMode mode, string? modelName)
        {
            return mode switch
            {
                ProductModelDialog.DialogMode.Edit => string.Format(_resourceLoader.GetString("ProductModelEditTitle"), modelName ?? string.Empty),
                ProductModelDialog.DialogMode.Duplicate => string.Format(_resourceLoader.GetString("ProductModelDuplicateTitle"), modelName ?? string.Empty),
                _ => _resourceLoader.GetString("ProductModelCreateTitle")
            };
        }

        private void ManageProductsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedModelDetail == null)
            {
                return;
            }

            if (App.Current is App app && app.CurrentWindow is PhoneStoreAdmin.MainWindow mainWindow)
            {
                mainWindow.NavigateToPage(ProductManagementPageTag, SelectedModelDetail);
            }
        }
    }
}
