using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Models;
using System;
using System.Collections.ObjectModel;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.View.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.View
{
    public sealed partial class BrandPage : Page
    {
        private IBrandService BrandService => App.GetService<IBrandService>();

        // Permission properties
        public bool CanAddBrand { get; }
        public bool CanEditBrand { get; }

        public ObservableCollection<BrandViewModel> Brands { get; } = new ObservableCollection<BrandViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;
        private ContentDialog? _currentDialog;

        private readonly ResourceLoader _resourceLoader;

        public BrandPage()
        {
            this._resourceLoader = new ResourceLoader();
            this.InitializeComponent();
            
            // Initialize permissions
            var session = UserSession.Instance;
            CanAddBrand = session.HasPermission("BRAND_ADD");
            CanEditBrand = session.HasPermission("BRAND_EDIT");
            
            this.Loaded += BrandPage_Loaded;
            this.Unloaded += BrandPage_Unloaded;
        }

        private void BrandPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            _currentDialog = null;
        }

        private void BrandPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadBrands();
        }

        private void LoadBrands()
        {
            if (!_isInitialized)
                return;

            try
            {
                Brands.Clear();

                var result = BrandService.GetBrandsFiltered(
                    SearchBox?.Text,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load brands.");
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                foreach (var brand in result.Brands)
                {
                    Brands.Add(brand);
                }

                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{Brands.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load brands", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Brands.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (BrandsListView != null)
                BrandsListView.Visibility = Brands.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            CurrentPage = 1;
            LoadBrands();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;
                CurrentPage = 1;
                LoadBrands();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error clearing filters", ex.Message);
            }
        }

        private async void ShowErrorDialog(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async void BtnCreate_Click(object sender, RoutedEventArgs e)
        {
            if (!CanAddBrand) return;
            
            var brandDialog = new BrandDialog();
            brandDialog.SetMode(BrandDialog.DialogMode.Add);

            var dialog = CreateContentDialog(brandDialog, _resourceLoader.GetString("AddBrandTitle"));
            dialog.PrimaryButtonText = _resourceLoader.GetString("DialogAdd");
            dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

            brandDialog.BrandSaved += (s, model) =>
            {
                if (model != null)
                {
                    LoadBrands(); // Refresh list
                }
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var ctrl = (BrandDialog)dialog.Content;
                if (!ctrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                try
                {
                    ctrl.Save();
                    LoadBrands();
                }
                catch (Exception ex)
                {
                    args.Cancel = true;
                    ShowErrorDialog("Error", ex.Message);
                }
            };

            _currentDialog = dialog;
            await dialog.ShowAsync();
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (!CanEditBrand) return;
            
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int brandId)) return;

            var brand = BrandService.GetBrandById(brandId);
            if (brand == null)
            {
                ShowErrorDialog("Error", "Brand not found.");
                return;
            }

            var viewModel = new BrandViewModel(brand);
            var brandDialog = new BrandDialog();
            brandDialog.SetMode(BrandDialog.DialogMode.Edit, viewModel);

            var dialog = CreateContentDialog(brandDialog, _resourceLoader.GetString("EditBrandTitle"));
            dialog.PrimaryButtonText = _resourceLoader.GetString("DialogUpdate");
            dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

            brandDialog.BrandSaved += (s, model) =>
            {
                if (model != null) LoadBrands();
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var ctrl = (BrandDialog)dialog.Content;
                if (!ctrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                try
                {
                    ctrl.Save();
                    LoadBrands();
                }
                catch (Exception ex)
                {
                    args.Cancel = true;
                    ShowErrorDialog("Error", ex.Message);
                }
            };

            _currentDialog = dialog;
            await dialog.ShowAsync();
        }

        private ContentDialog CreateContentDialog(ContentControl content, string title)
        {
            return new ContentDialog
            {
                Title = title,
                Content = content,
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Primary
            };
        }

        private void BtnActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element)
            {
                FlyoutBase.ShowAttachedFlyout(element);
            }
        }

        private void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage < TotalPages)
            {
                CurrentPage++;
                LoadBrands();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadBrands();
            }
        }
        private async void Grid_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid?.DataContext == null) return;

            var brandViewModel = grid.DataContext as BrandViewModel;
            if (brandViewModel == null) return;

            var brandDialog = new BrandDialog();
            brandDialog.SetMode(BrandDialog.DialogMode.Edit, brandViewModel);

            var dialog = CreateContentDialog(brandDialog, _resourceLoader.GetString("EditBrandTitle"));
            dialog.PrimaryButtonText = _resourceLoader.GetString("DialogUpdate");
            dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

            brandDialog.BrandSaved += (s, model) =>
            {
                if (model != null) LoadBrands();
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var ctrl = (BrandDialog)dialog.Content;
                if (!ctrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                try
                {
                    ctrl.Save();
                    LoadBrands();
                }
                catch (Exception ex)
                {
                    args.Cancel = true;
                    ShowErrorDialog("Error", ex.Message);
                }
            };

            _currentDialog = dialog;
            await dialog.ShowAsync();
        }

    }
}