using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.View
{
    /// <summary>
    /// Pricing Settings Page - Configure pricing parameters
    /// Requirements: 9.1, 9.2, 9.3, 9.4
    /// </summary>
    public sealed partial class PricingSettingsPage : Page
    {
        private readonly IDynamicPricingService _pricingService;
        private readonly PricingSettingsViewModel _viewModel;
        private bool _isInitializing = true;

        public PricingSettingsPage()
        {
            this.InitializeComponent();
            
            _pricingService = ServiceContainer.GetService<IDynamicPricingService>();
            _viewModel = new PricingSettingsViewModel();
            
            this.Loaded += PricingSettingsPage_Loaded;
        }

        private async void PricingSettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadConfigurationAsync();
        }

        /// <summary>
        /// Load current configuration from DynamicPricingService
        /// </summary>
        private async Task LoadConfigurationAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Đang tải cài đặt...";

                await Task.Run(() =>
                {
                    var config = _pricingService.GetConfiguration();
                    _viewModel.LoadFromConfiguration(config);
                });

                DispatcherQueue.TryEnqueue(() =>
                {
                    _isInitializing = true;
                    
                    // Set input values from ViewModel
                    DesiredMarginInput.Value = _viewModel.DesiredMarginPercent;
                    MinimumMarginInput.Value = _viewModel.MinimumMarginPercent;
                    VarianceThresholdInput.Value = _viewModel.VarianceThresholdPercent;
                    
                    _isInitializing = false;
                    
                    // Update preview calculator
                    UpdatePreviewCalculator();
                    
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Không thể tải cài đặt: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Validate input values
        /// </summary>
        private bool ValidateInputs()
        {
            // Validate Desired Margin
            if (double.IsNaN(DesiredMarginInput.Value) || DesiredMarginInput.Value < 0 || DesiredMarginInput.Value > 100)
            {
                ShowValidationError("Biên lợi nhuận mong muốn phải từ 0% đến 100%");
                return false;
            }

            // Validate Minimum Margin
            if (double.IsNaN(MinimumMarginInput.Value) || MinimumMarginInput.Value < 0 || MinimumMarginInput.Value > 100)
            {
                ShowValidationError("Biên lợi nhuận tối thiểu phải từ 0% đến 100%");
                return false;
            }

            // Validate Variance Threshold
            if (double.IsNaN(VarianceThresholdInput.Value) || VarianceThresholdInput.Value < -100 || VarianceThresholdInput.Value > 0)
            {
                ShowValidationError("Ngưỡng chênh lệch giá phải từ -100% đến 0%");
                return false;
            }

            // Validate Minimum Margin <= Desired Margin
            if (MinimumMarginInput.Value > DesiredMarginInput.Value)
            {
                ShowValidationError("Biên lợi nhuận tối thiểu không được lớn hơn biên lợi nhuận mong muốn");
                return false;
            }

            HideValidationError();
            return true;
        }

        private void ShowValidationError(string message)
        {
            ValidationErrorText.Text = message;
            ValidationErrorBorder.Visibility = Visibility.Visible;
        }

        private void HideValidationError()
        {
            ValidationErrorBorder.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;
                LoadingText.Text = "Đang lưu cài đặt...";

                // Update ViewModel from inputs
                _viewModel.DesiredMarginPercent = DesiredMarginInput.Value;
                _viewModel.MinimumMarginPercent = MinimumMarginInput.Value;
                _viewModel.VarianceThresholdPercent = VarianceThresholdInput.Value;

                var config = _viewModel.ToConfiguration();
                bool success = false;

                await Task.Run(() =>
                {
                    success = _pricingService.UpdateConfiguration(config);
                });

                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    
                    if (success)
                    {
                        await ShowSuccessDialogAsync("Đã lưu cài đặt thành công. Các tham số mới sẽ được áp dụng cho các tính toán giá tiếp theo.");
                    }
                    else
                    {
                        await ShowErrorDialogAsync("Không thể lưu cài đặt. Vui lòng thử lại.");
                    }
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    await ShowErrorDialogAsync($"Lỗi khi lưu cài đặt: {ex.Message}");
                });
            }
        }

        /// <summary>
        /// Navigate back to Dashboard
        /// </summary>
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = ServiceContainer.GetService<MainWindow>();
            mainWindow?.NavigateToPage("PricingDashboard");
        }

        /// <summary>
        /// Handle Desired Margin input change
        /// </summary>
        private void DesiredMarginInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;
            _viewModel.DesiredMarginPercent = args.NewValue;
            UpdatePreviewCalculator();
        }

        /// <summary>
        /// Handle Minimum Margin input change
        /// </summary>
        private void MinimumMarginInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;
            _viewModel.MinimumMarginPercent = args.NewValue;
            UpdatePreviewCalculator();
        }

        /// <summary>
        /// Handle Variance Threshold input change
        /// </summary>
        private void VarianceThresholdInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;
            _viewModel.VarianceThresholdPercent = args.NewValue;
            UpdatePreviewCalculator();
        }

        /// <summary>
        /// Handle preview input changes
        /// </summary>
        private void PreviewInput_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if (_isInitializing) return;
            UpdatePreviewCalculator();
        }

        /// <summary>
        /// Update preview calculator with current values
        /// </summary>
        private void UpdatePreviewCalculator()
        {
            try
            {
                decimal fifo = (decimal)PreviewFifoInput.Value;
                decimal nifo = (decimal)PreviewNifoInput.Value;
                decimal desiredMargin = (decimal)DesiredMarginInput.Value / 100m;
                decimal minimumMargin = (decimal)MinimumMarginInput.Value / 100m;
                decimal varianceThreshold = (decimal)VarianceThresholdInput.Value / 100m;

                if (fifo <= 0)
                {
                    PreviewVarianceText.Text = "N/A";
                    PreviewTrendText.Text = "N/A";
                    PreviewAutoProtectPrice.Text = "N/A";
                    PreviewClearancePrice.Text = "N/A";
                    PreviewAlertBorder.Visibility = Visibility.Collapsed;
                    return;
                }

                // Calculate variance
                decimal variance = (nifo - fifo) / fifo;
                decimal variancePercent = variance * 100;

                // Update variance display
                string varianceSign = variance >= 0 ? "+" : "";
                PreviewVarianceText.Text = $"{varianceSign}{variancePercent:F2}%";
                PreviewVarianceText.Foreground = variance >= 0 
                    ? new SolidColorBrush(Colors.Green) 
                    : new SolidColorBrush(Colors.Red);

                // Determine market trend
                string trendText;
                SolidColorBrush trendColor;
                
                if (variance > 0)
                {
                    trendText = "↗ Tăng (UP)";
                    trendColor = new SolidColorBrush(Colors.Green);
                }
                else if (variance >= -0.05m && variance <= 0)
                {
                    trendText = "→ Ổn định (STABLE)";
                    trendColor = new SolidColorBrush(Colors.Gray);
                }
                else
                {
                    trendText = "↘ Giảm (DOWN)";
                    trendColor = new SolidColorBrush(Colors.Red);
                }

                PreviewTrendText.Text = trendText;
                PreviewTrendText.Foreground = trendColor;

                // Calculate AUTO_PROTECT price: MAX(FIFO, NIFO) × (1 + DesiredMargin)
                decimal maxCost = Math.Max(fifo, nifo);
                decimal autoProtectPrice = maxCost * (1 + desiredMargin);
                PreviewAutoProtectPrice.Text = autoProtectPrice.ToString("N0") + "đ";

                // Calculate CLEARANCE price: NIFO × (1 + MinimumMargin)
                decimal clearancePrice = nifo * (1 + minimumMargin);
                PreviewClearancePrice.Text = clearancePrice.ToString("N0") + "đ";

                // Show alert warning if variance is below threshold
                if (variance <= varianceThreshold)
                {
                    PreviewAlertBorder.Visibility = Visibility.Visible;
                }
                else
                {
                    PreviewAlertBorder.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                // Ignore calculation errors during input
            }
        }

        /// <summary>
        /// Show success dialog
        /// </summary>
        private async Task ShowSuccessDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Thành công",
                Content = message,
                CloseButtonText = "Đóng",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
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
