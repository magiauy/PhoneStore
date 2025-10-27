using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.View.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.View
{
    public sealed partial class PromotionsPage : Page
    {
        private IPromotionService PromotionService => App.GetService<IPromotionService>();

        public ObservableCollection<PromotionViewModel> Promotions { get; } = new ObservableCollection<PromotionViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;
        private ContentDialog? _currentDialog;

        private readonly ResourceLoader _resourceLoader;

        public PromotionsPage()
        {
            this._resourceLoader = new ResourceLoader();
            this.InitializeComponent();
            this.Loaded += PromotionsPage_Loaded;
            this.Unloaded += PromotionsPage_Unloaded;
        }

        private void PromotionsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            _currentDialog = null;
        }

        private void PromotionsPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadPromotions();
        }

        private void LoadPromotions()
        {
            if (!_isInitialized)
                return;

            try
            {
                Promotions.Clear();
                string nameFilter = SearchBox?.Text ?? string.Empty;
                bool? isActiveFilter = null;
                if (StatusFilterBox?.SelectedIndex == 1)
                    isActiveFilter = true;
                else if (StatusFilterBox?.SelectedIndex == 2)
                    isActiveFilter = false;

                DateTime? startDateFilter = null;
                var dateOffsetFrom = StartDatePicker?.Date;
                if (dateOffsetFrom.HasValue && dateOffsetFrom.Value.Year > 1900)
                    startDateFilter = dateOffsetFrom.Value.DateTime.Date;

                DateTime? endDateFilter = null;
                var dateOffsetTo = EndDatePicker?.Date;
                if (dateOffsetTo.HasValue && dateOffsetTo.Value.Year > 1900)
                    endDateFilter = dateOffsetTo.Value.DateTime.Date.AddDays(1).AddSeconds(-1);

                var result = PromotionService.GetPromotionsFiltered(
                    nameFilter,
                    isActiveFilter,
                    startDateFilter,
                    endDateFilter,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load promotions.");
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                Promotions.Clear();
                foreach (var promotion in result.Promotions)
                {
                    Promotions.Add(promotion);
                }

                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{Promotions.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load promotions", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = Promotions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (PromotionsListView != null)
                PromotionsListView.Visibility = Promotions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            CurrentPage = 1;
            LoadPromotions();
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadPromotions();
        }

        private void FilterChange(object sender, DatePickerValueChangedEventArgs e)
        {
            CurrentPage = 1;
            LoadPromotions();
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilterPanel != null)
            {
                FilterPanel.Visibility = FilterPanel.Visibility == Visibility.Collapsed
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;

                if (StatusFilterBox != null)
                    StatusFilterBox.SelectedIndex = 0;

                if (StartDatePicker != null)
                    StartDatePicker.SelectedDate = null;

                if (EndDatePicker != null)
                    EndDatePicker.SelectedDate = null;

                CurrentPage = 1;
                LoadPromotions();
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
            var promotionDialog = new PromotionDialog();
            promotionDialog.SetMode(PromotionDialog.DialogMode.Add);

            var dialog = CreateContentDialog(promotionDialog, _resourceLoader.GetString("AddPromotionTitle"));
            dialog.PrimaryButtonText = _resourceLoader.GetString("DialogAdd");
            dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

            promotionDialog.PromotionSaved += (s, model) =>
            {
                if (model != null)
                {
                    LoadPromotions();
                }
            };

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var ctrl = (PromotionDialog)dialog.Content;
                if (!ctrl.IsValid())
                {
                    args.Cancel = true;
                    return;
                }
                try {
                    ctrl.Save();
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

        private void NavigateToEditPromotion_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionId)) return;

            Frame?.Navigate(typeof(PromotionEditPage), promotionId);
        }

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionId)) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("ConfirmActivateSupplierTitle"),
                    Content = _resourceLoader.GetString("ConfirmActivateSupplierContent"),
                    PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                    CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    PromotionService.ActivatePromotion(promotionId);
                    LoadPromotions();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to activate promotion: {ex.Message}");
            }
        }

        private async void BtnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionId)) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("ConfirmDeactivateSupplierTitle"),
                    Content = _resourceLoader.GetString("ConfirmDeactivateSupplierContent"),
                    PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                    CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    PromotionService.DeactivatePromotion(promotionId);
                    LoadPromotions();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to deactivate promotion: {ex.Message}");
            }
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
                LoadPromotions();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPromotions();
            }
        }
    }
}
