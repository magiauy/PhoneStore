using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using PhoneStoreAdmin.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreAdmin.View.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.View
{
    public sealed partial class PromotionCodesPage : Page
    {
        private IPromotionCodeService PromotionCodeService => App.GetService<IPromotionCodeService>();
        private IPromotionService PromotionService => App.GetService<IPromotionService>();

        public ObservableCollection<PromotionCodeViewModel> PromotionCodes { get; } = new ObservableCollection<PromotionCodeViewModel>();
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;

        private bool _isInitialized = false;
        private ContentDialog? _currentDialog;
        private List<string> _promotionNames = new List<string>();
        private readonly List<EventHandler<PromotionCodeViewModel>> _eventHandlers = new List<EventHandler<PromotionCodeViewModel>>();
        private System.Threading.Timer? _searchTimer;

        private readonly ResourceLoader _resourceLoader;

        public PromotionCodesPage()
        {
            this._resourceLoader = new ResourceLoader();
            this.InitializeComponent();
            this.Loaded += PromotionCodesPage_Loaded;
            this.Unloaded += PromotionCodesPage_Unloaded;
        }

        private void PromotionCodesPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CleanupResources();
        }

        private void CleanupResources()
        {
            _searchTimer?.Dispose();
            _searchTimer = null;
            _currentDialog = null;
            _eventHandlers.Clear();
            _promotionNames.Clear();
        }

        private void PromotionCodesPage_Loaded(object sender, RoutedEventArgs e)
        {
            _isInitialized = true;
            LoadPromotionNames();
            LoadPromotionCodes();
        }

        private void LoadPromotionNames()
        {
            try
            {
                var promotions = PromotionService.GetAll();
                _promotionNames = promotions.Select(p => p.Name ?? string.Empty).Where(name => !string.IsNullOrEmpty(name)).ToList();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to load promotions: {ex.Message}");
            }
        }

        private void LoadPromotionCodes()
        {
            if (!_isInitialized)
                return;

            try
            {
                PromotionCodes.Clear();
                
                // Collect all filter values
                string codeFilter = SearchBox?.Text ?? string.Empty;
                string promotionNameFilter = PromotionNameFilterBox?.Text ?? string.Empty;
                
                bool? isActiveFilter = null;
                if (StatusFilterBox?.SelectedIndex == 1)
                    isActiveFilter = true;
                else if (StatusFilterBox?.SelectedIndex == 2)
                    isActiveFilter = false;

                // Amount filters
                decimal? minDiscountAmount = !double.IsNaN(MinDiscountAmountBox?.Value ?? double.NaN) ? (decimal?)MinDiscountAmountBox?.Value : null;
                decimal? maxDiscountAmount = !double.IsNaN(MaxDiscountAmountBox?.Value ?? double.NaN) ? (decimal?)MaxDiscountAmountBox?.Value : null;
                decimal? minMinimumAmount = !double.IsNaN(MinMinimumAmountBox?.Value ?? double.NaN) ? (decimal?)MinMinimumAmountBox?.Value : null;
                decimal? maxMinimumAmount = !double.IsNaN(MaxMinimumAmountBox?.Value ?? double.NaN) ? (decimal?)MaxMinimumAmountBox?.Value : null;

                // Usage limit filters
                int? minUsageLimit = !double.IsNaN(MinUsageLimitBox?.Value ?? double.NaN) ? (int?)MinUsageLimitBox?.Value : null;
                int? maxUsageLimit = !double.IsNaN(MaxUsageLimitBox?.Value ?? double.NaN) ? (int?)MaxUsageLimitBox?.Value : null;

                var result = PromotionCodeService.GetPromotionCodesWithAdvancedFilter(
                    string.IsNullOrWhiteSpace(codeFilter) ? null : codeFilter,
                    null, // promotionId
                    string.IsNullOrWhiteSpace(promotionNameFilter) ? null : promotionNameFilter,
                    isActiveFilter,
                    minDiscountAmount,
                    maxDiscountAmount,
                    minMinimumAmount,
                    maxMinimumAmount,
                    minUsageLimit,
                    maxUsageLimit,
                    CurrentPage,
                    PageSize);

                if (result == null)
                {
                    ShowErrorDialog("Error", "Failed to load promotion codes.");
                    return;
                }

                TotalPages = result.Info.TotalPages;
                TotalRecords = result.Info.TotalRecords;

                PromotionCodes.Clear();
                foreach (var promotionCode in result.PromotionCodes)
                {
                    PromotionCodes.Add(promotionCode);
                }

                if (PageInfoText != null)
                    PageInfoText.Text = $"{CurrentPage} / {TotalPages}";

                if (PreviousPageButton != null)
                    PreviousPageButton.IsEnabled = CurrentPage > 1;

                if (NextPageButton != null)
                    NextPageButton.IsEnabled = CurrentPage < TotalPages;

                if (RecordCountText != null)
                    RecordCountText.Text = $"{PromotionCodes.Count} / {TotalRecords}";

                UpdateEmptyStateVisibility();
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Failed to load promotion codes", ex.Message);
            }
        }

        private void UpdateEmptyStateVisibility()
        {
            if (EmptyStatePanel != null)
                EmptyStatePanel.Visibility = PromotionCodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (PromotionCodesListView != null)
                PromotionCodesListView.Visibility = PromotionCodes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            _searchTimer?.Dispose();
            _searchTimer = new System.Threading.Timer(_ =>
            {
                this.DispatcherQueue.TryEnqueue(() =>
                {
                    CurrentPage = 1;
                    LoadPromotionCodes();
                });
            }, null, 500, System.Threading.Timeout.Infinite);
        }

        private void FilterChange(object sender, RoutedEventArgs e)
        {
            CurrentPage = 1;
            LoadPromotionCodes();
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
                // Clear all filter controls (basic + advanced)
                if (SearchBox != null)
                    SearchBox.Text = string.Empty;

                if (PromotionNameFilterBox != null) 
                    PromotionNameFilterBox.Text = string.Empty;

                if (StatusFilterBox != null)
                    StatusFilterBox.SelectedIndex = 0;

                // Clear amount range filters
                if (MinDiscountAmountBox != null) 
                    MinDiscountAmountBox.Value = double.NaN;
                if (MaxDiscountAmountBox != null) 
                    MaxDiscountAmountBox.Value = double.NaN;
                if (MinMinimumAmountBox != null) 
                    MinMinimumAmountBox.Value = double.NaN;
                if (MaxMinimumAmountBox != null) 
                    MaxMinimumAmountBox.Value = double.NaN;

                // Clear usage limit filters
                if (MinUsageLimitBox != null) 
                    MinUsageLimitBox.Value = double.NaN;
                if (MaxUsageLimitBox != null) 
                    MaxUsageLimitBox.Value = double.NaN;

                CurrentPage = 1;
                LoadPromotionCodes();
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
            try
            {
                var promotionCodeDialog = new PromotionCodeDialog();
                promotionCodeDialog.SetMode(PromotionCodeDialog.DialogMode.Add);

                var dialog = CreateContentDialog(promotionCodeDialog, _resourceLoader.GetString("AddPromotionCodeTitle"));
                dialog.PrimaryButtonText = _resourceLoader.GetString("DialogAdd");
                dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

                EventHandler<PromotionCodeViewModel> handler = (s, model) =>
                {
                    if (model != null)
                    {
                        LoadPromotionCodes();
                    }
                };
                promotionCodeDialog.PromotionCodeSaved += handler;
                _eventHandlers.Add(handler);

                dialog.PrimaryButtonClick += (s, args) =>
                {
                    var ctrl = (PromotionCodeDialog)dialog.Content;
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
            catch (Exception ex)
            {
                Logger.Error("Critical error in BtnCreate_Click", ex);
                ShowErrorDialog("Error", $"Failed to open promotion code dialog: {ex.Message}");
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            var promotionCode = PromotionCodeService.GetPromotionCodeById(promotionCodeId);
            if (promotionCode == null)
            {
                ShowErrorDialog("Error", "Promotion code not found.");
                return;
            }

            var viewModel = new PromotionCodeViewModel(promotionCode);
            var promotionCodeDialog = new PromotionCodeDialog();
            promotionCodeDialog.SetMode(PromotionCodeDialog.DialogMode.Edit, viewModel);

            var dialog = CreateContentDialog(promotionCodeDialog, _resourceLoader.GetString("EditPromotionCodeTitle"));
            dialog.PrimaryButtonText = _resourceLoader.GetString("DialogUpdate");
            dialog.CloseButtonText = _resourceLoader.GetString("DialogCancel");

            EventHandler<PromotionCodeViewModel> editHandler = (s, model) =>
            {
                if (model != null) LoadPromotionCodes();
            };
            promotionCodeDialog.PromotionCodeSaved += editHandler;
            _eventHandlers.Add(editHandler);

            dialog.PrimaryButtonClick += (s, args) =>
            {
                var ctrl = (PromotionCodeDialog)dialog.Content;
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

        private async void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("ConfirmActivatePromotionCodeTitle"),
                    Content = _resourceLoader.GetString("ConfirmActivatePromotionCodeContent"),
                    PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                    CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    PromotionCodeService.ActivatePromotionCode(promotionCodeId);
                    LoadPromotionCodes();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to activate promotion code: {ex.Message}");
            }
        }

        private async void BtnDeactivate_Click(object sender, RoutedEventArgs e)
        {
            var idStr = (sender as MenuFlyoutItem)?.Tag?.ToString();
            if (!int.TryParse(idStr, out int promotionCodeId)) return;

            try
            {
                var dialog = new ContentDialog
                {
                    Title = _resourceLoader.GetString("ConfirmDeactivatePromotionCodeTitle"),
                    Content = _resourceLoader.GetString("ConfirmDeactivatePromotionCodeContent"),
                    PrimaryButtonText = _resourceLoader.GetString("BtnConfirm"),
                    CloseButtonText = _resourceLoader.GetString("BtnCancel"),
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    PromotionCodeService.DeactivatePromotionCode(promotionCodeId);
                    LoadPromotionCodes();
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog("Error", $"Failed to deactivate promotion code: {ex.Message}");
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
                LoadPromotionCodes();
            }
        }

        private void BtnLastPage_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPage > 1)
            {
                CurrentPage--;
                LoadPromotionCodes();
            }
        }

        #region Filter Event Handlers

        private void PromotionNameFilter_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var query = sender.Text?.ToLower() ?? string.Empty;
                var suggestions = _promotionNames.Where(name => 
                    name.ToLower().Contains(query)).Take(10).ToList();
                
                sender.ItemsSource = suggestions;
            }
        }

        private void PromotionNameFilter_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            sender.Text = args.SelectedItem?.ToString() ?? string.Empty;
            CurrentPage = 1;
            LoadPromotionCodes();
        }

        private void PromotionNameFilter_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            CurrentPage = 1;
            LoadPromotionCodes();
        }

        private void AmountFilter_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Validate range: Min should not be greater than Max
            if (sender == MinDiscountAmountBox && MaxDiscountAmountBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MaxDiscountAmountBox.Value) && args.NewValue > MaxDiscountAmountBox.Value)
                {
                    MaxDiscountAmountBox.Value = args.NewValue;
                }
            }
            else if (sender == MaxDiscountAmountBox && MinDiscountAmountBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MinDiscountAmountBox.Value) && args.NewValue < MinDiscountAmountBox.Value)
                {
                    MinDiscountAmountBox.Value = args.NewValue;
                }
            }
            else if (sender == MinMinimumAmountBox && MaxMinimumAmountBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MaxMinimumAmountBox.Value) && args.NewValue > MaxMinimumAmountBox.Value)
                {
                    MaxMinimumAmountBox.Value = args.NewValue;
                }
            }
            else if (sender == MaxMinimumAmountBox && MinMinimumAmountBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MinMinimumAmountBox.Value) && args.NewValue < MinMinimumAmountBox.Value)
                {
                    MinMinimumAmountBox.Value = args.NewValue;
                }
            }

            CurrentPage = 1;
            LoadPromotionCodes();
        }

        private void UsageLimitFilter_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Validate range: Min should not be greater than Max
            if (sender == MinUsageLimitBox && MaxUsageLimitBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MaxUsageLimitBox.Value) && args.NewValue > MaxUsageLimitBox.Value)
                {
                    MaxUsageLimitBox.Value = args.NewValue;
                }
            }
            else if (sender == MaxUsageLimitBox && MinUsageLimitBox != null)
            {
                if (!double.IsNaN(args.NewValue) && !double.IsNaN(MinUsageLimitBox.Value) && args.NewValue < MinUsageLimitBox.Value)
                {
                    MinUsageLimitBox.Value = args.NewValue;
                }
            }

            CurrentPage = 1;
            LoadPromotionCodes();
        }

        #endregion
    }
}