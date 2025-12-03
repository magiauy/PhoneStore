using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    /// <summary>
    /// Pricing Dashboard Page - Displays pricing overview, trends and recent activity
    /// Requirements: 7.1, 7.2, 7.3
    /// </summary>
    public sealed partial class PricingDashboardPage : Page
    {
        private readonly IDynamicPricingService _pricingService;
        private readonly IPricingAlertService _alertService;
        private readonly PricingDashboardViewModel _viewModel;

        public PricingDashboardPage()
        {
            this.InitializeComponent();
            
            _pricingService = ServiceContainer.GetService<IDynamicPricingService>();
            _alertService = ServiceContainer.GetService<IPricingAlertService>();
            _viewModel = new PricingDashboardViewModel();
            
            this.Loaded += PricingDashboardPage_Loaded;
        }

        private async void PricingDashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        /// <summary>
        /// Load dashboard data from DynamicPricingService
        /// </summary>
        private async Task LoadDashboardDataAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    var dashboardData = _pricingService.GetDashboardData();
                    _viewModel.UpdateFromDashboardData(dashboardData);
                });

                // Update UI on main thread
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateSummaryCards();
                    UpdateTrendList();
                    UpdateRecentActivity();
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Không thể tải dữ liệu: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Update summary cards with dashboard data
        /// </summary>
        private void UpdateSummaryCards()
        {
            TotalProductsText.Text = _viewModel.TotalProducts.ToString("N0");
            PriceIncreaseText.Text = _viewModel.ProductsWithPriceIncrease.ToString("N0");
            ClearanceText.Text = _viewModel.ProductsInClearance.ToString("N0");
            PendingAlertsText.Text = _viewModel.PendingAlerts.ToString("N0");
        }

        /// <summary>
        /// Update trend list with product FIFO/NIFO data
        /// </summary>
        private void UpdateTrendList()
        {
            if (_viewModel.TrendItems.Count == 0)
            {
                TrendListView.Visibility = Visibility.Collapsed;
                TrendEmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                TrendListView.Visibility = Visibility.Visible;
                TrendEmptyState.Visibility = Visibility.Collapsed;
                TrendListView.ItemsSource = _viewModel.TrendItems;
            }
        }

        /// <summary>
        /// Update recent activity list with color coding (green/red)
        /// </summary>
        private void UpdateRecentActivity()
        {
            if (_viewModel.RecentActivities.Count == 0)
            {
                RecentActivityListView.Visibility = Visibility.Collapsed;
                ActivityEmptyState.Visibility = Visibility.Visible;
            }
            else
            {
                RecentActivityListView.Visibility = Visibility.Visible;
                ActivityEmptyState.Visibility = Visibility.Collapsed;
                RecentActivityListView.ItemsSource = _viewModel.RecentActivities;
            }
        }

        /// <summary>
        /// Refresh button click handler - implements refresh logic
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDashboardDataAsync();
        }

        /// <summary>
        /// Navigate to Alert Center page
        /// </summary>
        private void AlertCenterButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = ServiceContainer.GetService<MainWindow>();
            mainWindow?.NavigateToPage("PricingAlertCenter");
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
    }
}
