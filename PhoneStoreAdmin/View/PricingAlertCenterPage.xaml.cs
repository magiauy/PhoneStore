using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.ViewModels;
using PhoneStoreRepository.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    /// <summary>
    /// Pricing Alert Center Page - Manages and resolves pricing alerts
    /// Requirements: 4.2, 5.1, 5.2, 5.3, 5.4, 7.5
    /// </summary>
    public sealed partial class PricingAlertCenterPage : Page
    {
        private readonly IPricingAlertService _alertService;
        private readonly PricingAlertCenterViewModel _viewModel;
        private PricingAlertItemViewModel? _selectedAlert;

        public PricingAlertCenterPage()
        {
            this.InitializeComponent();
            
            _alertService = ServiceContainer.GetService<IPricingAlertService>();
            _viewModel = new PricingAlertCenterViewModel();
            
            this.Loaded += PricingAlertCenterPage_Loaded;
        }

        private async void PricingAlertCenterPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAlertsAsync();
        }

        /// <summary>
        /// Load pending alerts from PricingAlertService
        /// </summary>
        private async Task LoadAlertsAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Đang tải danh sách cảnh báo...";

                // Fetch data on background thread
                var alerts = await Task.Run(() => _alertService.GetPendingAlerts());

                // Update UI on dispatcher thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    _viewModel.LoadAlerts(alerts);
                    UpdateAlertsList();
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Không thể tải danh sách cảnh báo: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Update alerts list UI
        /// </summary>
        private void UpdateAlertsList()
        {
            if (_viewModel.Alerts.Count == 0)
            {
                AlertsListView.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                AlertsListView.Visibility = Visibility.Visible;
                EmptyState.Visibility = Visibility.Collapsed;
                AlertsListView.ItemsSource = _viewModel.Alerts;
            }
            
            // Reset selection
            _selectedAlert = null;
            DetailPanel.Visibility = Visibility.Collapsed;
            NoSelectionState.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handle alert selection change - show detail panel
        /// </summary>
        private void AlertsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AlertsListView.SelectedItem is PricingAlertItemViewModel alert)
            {
                _selectedAlert = alert;
                UpdateDetailPanel(alert);
                NoSelectionState.Visibility = Visibility.Collapsed;
                DetailPanel.Visibility = Visibility.Visible;
            }
            else
            {
                _selectedAlert = null;
                NoSelectionState.Visibility = Visibility.Visible;
                DetailPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Update detail panel with selected alert info
        /// </summary>
        private void UpdateDetailPanel(PricingAlertItemViewModel alert)
        {
            DetailProductName.Text = alert.ProductName;
            DetailProductSku.Text = alert.ProductSku;
            DetailVariance.Text = alert.VarianceDisplay;
            DetailFifo.Text = alert.CostFifoDisplay;
            DetailNifo.Text = alert.CostNifoDisplay;
            DetailStock.Text = alert.CurrentStock.ToString("N0");
            DetailPotentialLoss.Text = alert.PotentialLossDisplay;
            DetailCreatedAt.Text = alert.CreatedAt.ToString("dd/MM/yyyy HH:mm");
        }

        /// <summary>
        /// Handle "Giữ giá" button click with confirmation dialog
        /// Requirements: 5.1
        /// </summary>
        private async void HoldPriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAlert == null) return;

            var dialog = new ContentDialog
            {
                Title = "Xác nhận giữ giá",
                Content = $"Bạn có chắc chắn muốn giữ nguyên giá bán hiện tại cho sản phẩm \"{_selectedAlert.ProductName}\"?\n\nGiá bán sẽ không thay đổi và cảnh báo sẽ được đánh dấu là đã xử lý.",
                PrimaryButtonText = "Giữ giá",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ResolveAlertAsHoldAsync(_selectedAlert.AlertId);
            }
        }

        /// <summary>
        /// Handle "Kích hoạt xả hàng" button click with confirmation dialog
        /// Requirements: 5.2, 5.3
        /// </summary>
        private async void ClearanceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedAlert == null) return;

            var dialog = new ContentDialog
            {
                Title = "Xác nhận kích hoạt xả hàng",
                Content = $"Bạn có chắc chắn muốn kích hoạt chế độ xả hàng cho sản phẩm \"{_selectedAlert.ProductName}\"?\n\n• Chế độ định giá sẽ chuyển sang CLEARANCE\n• Giá bán sẽ được tính lại theo công thức: NIFO × (1 + Biên lợi nhuận tối thiểu)\n• Khách hàng sẽ thấy nhãn \"Giá ưu đãi xả kho\"",
                PrimaryButtonText = "Kích hoạt xả hàng",
                CloseButtonText = "Hủy",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                await ResolveAlertAsClearanceAsync(_selectedAlert.AlertId);
            }
        }

        /// <summary>
        /// Resolve alert as Hold - keep current price
        /// Requirements: 5.1
        /// </summary>
        private async Task ResolveAlertAsHoldAsync(int alertId)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Đang xử lý...";

                var adminId = UserSession.Instance.Account?.Id ?? 0;
                bool success = false;

                await Task.Run(() =>
                {
                    success = _alertService.ResolveAsHold(alertId, adminId, "Giữ giá theo quyết định Admin");
                });

                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    
                    if (success)
                    {
                        await ShowSuccessDialogAsync("Đã giữ giá thành công", "Giá bán sản phẩm được giữ nguyên và cảnh báo đã được đánh dấu là đã xử lý.");
                        await LoadAlertsAsync(); // Refresh list
                    }
                    else
                    {
                        await ShowErrorDialogAsync("Không thể xử lý cảnh báo. Vui lòng thử lại.");
                    }
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Lỗi: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Resolve alert as Clearance - switch to clearance mode and recalculate price
        /// Requirements: 5.2, 5.3, 5.4
        /// </summary>
        private async Task ResolveAlertAsClearanceAsync(int alertId)
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Đang kích hoạt xả hàng...";

                var adminId = UserSession.Instance.Account?.Id ?? 0;
                bool success = false;

                await Task.Run(() =>
                {
                    success = _alertService.ResolveAsClearance(alertId, adminId, "Kích hoạt xả hàng theo quyết định Admin");
                });

                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    
                    if (success)
                    {
                        await ShowSuccessDialogAsync("Đã kích hoạt xả hàng", "Sản phẩm đã chuyển sang chế độ CLEARANCE và giá bán đã được tính lại.");
                        await LoadAlertsAsync(); // Refresh list
                    }
                    else
                    {
                        await ShowErrorDialogAsync("Không thể kích hoạt xả hàng. Vui lòng thử lại.");
                    }
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Lỗi: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Refresh button click handler
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadAlertsAsync();
        }

        /// <summary>
        /// Back button click handler - navigate to Pricing Dashboard
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = ServiceContainer.GetService<MainWindow>();
            mainWindow?.NavigateToPage("PricingDashboard");
        }

        /// <summary>
        /// Show error dialog
        /// </summary>
        private async Task ShowErrorDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Lỗi",
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// Show success dialog
        /// </summary>
        private async Task ShowSuccessDialogAsync(string title, string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
