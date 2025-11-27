using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStore.Services;
using PhoneStore.Services.Helpers;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Utils;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ReportsPage : Page
    {
        // ResourceLoader for localization
        private readonly ResourceLoader _resourceLoader = new();

        // Services
        private readonly IPurchaseOrderService? _purchaseOrderService;
        private readonly ISupplierService? _supplierService;

        // Data collections
        public ObservableCollection<TopSupplierViewModel> TopSuppliers { get; set; }

        // Filter state
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private PoStatus? _selectedStatus;

        public ReportsPage()
        {
            this.InitializeComponent();
            TopSuppliers = new ObservableCollection<TopSupplierViewModel>();
            TopSuppliersListView.ItemsSource = TopSuppliers;

            this.DataContext = this;

            // Get services from ServiceContainer
            _purchaseOrderService = ServiceContainer.GetService<IPurchaseOrderService>();
            _supplierService = ServiceContainer.GetService<ISupplierService>();

            // Initialize filters
            StatusFilterComboBox.SelectedIndex = 0; // All status

            // Set default date range (last 30 days)
            _toDate = DateTime.Now.Date;
            _fromDate = _toDate.Value.AddDays(-30);

            // Configure date pickers with DD/MM/YYYY format
            if (ToDatePicker != null)
            {
                ToDatePicker.Date = new DateTimeOffset(_toDate.Value);
                // Set format to display as DD/MM/YYYY
                ToDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
            }
            if (FromDatePicker != null)
            {
                FromDatePicker.Date = new DateTimeOffset(_fromDate.Value);
                // Set format to display as DD/MM/YYYY
                FromDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadStatistics();
        }

        private void LoadStatistics()
        {
            try
            {
                if (_purchaseOrderService == null || _supplierService == null)
                    return;

                // Get filtered purchase orders
                var result = _purchaseOrderService.GetPurchaseOrdersFiltered(
                    null, // supplierName
                    null, // supplierId
                    null, // createdBy
                    _selectedStatus,
                    null, // note
                    _fromDate,
                    _toDate,
                    null, // minAmount
                    null, // maxAmount
                    1, // page
                    10000 // large pageSize to get all records
                );

                if (result == null || result.PurchaseOrders == null)
                    return;

                var orders = result.PurchaseOrders.ToList();

                // Calculate summary statistics
                int totalOrders = orders.Count;
                decimal totalAmount = orders.Sum(o => o.TotalAmount);
                int draftOrders = orders.Count(o => o.Status == PoStatus.DRAFT);
                int receivedOrders = orders.Count(o => o.Status == PoStatus.RECEIVED);

                // Update UI
                if (TotalOrdersText != null)
                    TotalOrdersText.Text = totalOrders.ToString("N0");

                if (TotalAmountText != null)
                    TotalAmountText.Text = totalAmount.ToString("N0");

                if (DraftOrdersText != null)
                    DraftOrdersText.Text = draftOrders.ToString("N0");

                if (ReceivedOrdersText != null)
                    ReceivedOrdersText.Text = receivedOrders.ToString("N0");

                // Load Top Suppliers
                LoadTopSuppliers(orders);

                // Draw Monthly Chart
                DrawMonthlyChart(orders);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load statistics", ex);
                ShowErrorDialog("Lỗi", $"Không thể tải thống kê: {ex.Message}");
            }
        }

        private void LoadTopSuppliers(List<PurchaseOrderViewModel> orders)
        {
            try
            {
                // Group by supplier and calculate totals
                var supplierStats = orders
                    .GroupBy(o => o.SupplierId)
                    .Select(g => new
                    {
                        SupplierId = g.Key,
                        OrderCount = g.Count(),
                        TotalAmount = g.Sum(o => o.TotalAmount)
                    })
                    .OrderByDescending(s => s.TotalAmount)
                    .Take(5)
                    .ToList();

                TopSuppliers.Clear();
                int rank = 1;
                foreach (var stat in supplierStats)
                {
                    var supplier = _supplierService?.GetSupplierById(stat.SupplierId);
                    TopSuppliers.Add(new TopSupplierViewModel
                    {
                        Rank = rank++,
                        SupplierName = supplier?.Name ?? "Unknown",
                        OrderCount = stat.OrderCount,
                        TotalAmount = stat.TotalAmount.ToString("N0")
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load top suppliers", ex);
            }
        }

        private void DrawMonthlyChart(List<PurchaseOrderViewModel> orders)
        {
            try
            {
                if (MonthlyChartGrid == null) return;

                MonthlyChartGrid.Children.Clear();

                // 1. Prepare Data
                var monthlyData = orders
                    .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        Amount = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(m => m.Date)
                    .ToList();

                // 2. Handle No Data
                if (!monthlyData.Any())
                {
                    var noDataText = new TextBlock
                    {
                        Text = _resourceLoader.GetString("Reports_NoDataAvailable") ?? "Chưa có dữ liệu", // Fallback text
                        Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
                        Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] as Brush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    MonthlyChartGrid.Children.Add(noDataText);
                    return;
                }

                // 3. Calculation Constants
                decimal maxAmount = monthlyData.Max(m => m.Amount);
                if (maxAmount == 0) maxAmount = 1; // Avoid divide by zero

                double chartHeight = 260; // Chiều cao tổng của khu vực vẽ
                double barWidth = 32;

                // 4. Create Main Container (Holds all bars horizontally)
                var chartContainer = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 24, // Khoảng cách giữa các cột
                    HorizontalAlignment = HorizontalAlignment.Center, // Căn giữa biểu đồ
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Padding = new Thickness(0, 0, 0, 10)
                };

                // 5. Draw Bars
                foreach (var data in monthlyData)
                {
                    // Calculate height percentage (min 4px visibility)
                    double heightPercentage = (double)(data.Amount / maxAmount);
                    double barHeight = Math.Max(4, heightPercentage * (chartHeight - 50)); // Trừ 50px cho Text

                    // Column Container (Holds: Amount -> Bar -> Month)
                    var columnStack = new StackPanel
                    {
                        Spacing = 8,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };

                    // A. Amount Label (Top)
                    var amountText = new TextBlock
                    {
                        Text = (data.Amount / 1_000_000).ToString("N1") + "M",
                        Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                        Foreground = Application.Current.Resources["BrushPrimary"] as Brush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Opacity = heightPercentage > 0.1 ? 1 : 0 // Ẩn số nếu cột quá thấp cho đỡ rối
                    };

                    // B. The Bar (Middle)
                    var barBorder = new Border
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Background = Application.Current.Resources["BrushPrimary"] as Brush, // Màu chủ đạo của WinUI
                        CornerRadius = new CornerRadius(4), // Bo góc mềm mại
                        Opacity = 0.9
                    };

                    // Tooltip modern styling
                    var toolTipContent = new StackPanel { Spacing = 4 };
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"Tháng {data.Date:MM/yyyy}",
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    });
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"{data.Count} đơn hàng",
                        Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                    });
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"{data.Amount:N0} ₫",
                        Foreground = Application.Current.Resources["SystemFillColorSuccessBrush"] as Brush
                    });
                    ToolTipService.SetToolTip(barBorder, toolTipContent);


                    // C. Month Label (Bottom)
                    var monthText = new TextBlock
                    {
                        Text = data.Date.ToString("MM"),
                        Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                        Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    // Add elements to column
                    columnStack.Children.Add(amountText);
                    columnStack.Children.Add(barBorder);
                    columnStack.Children.Add(monthText);

                    // Add column to chart
                    chartContainer.Children.Add(columnStack);
                }

                MonthlyChartGrid.Children.Add(chartContainer);
            }
            catch (Exception ex)
            {
                // Debug.WriteLine(ex.Message); // Log nếu cần
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
        }

        private void DateFilter_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender == FromDatePicker && args.NewDate.HasValue)
            {
                _fromDate = args.NewDate.Value.DateTime.Date;
            }
            else if (sender == ToDatePicker && args.NewDate.HasValue)
            {
                _toDate = args.NewDate.Value.DateTime.Date.AddDays(1).AddSeconds(-1);
            }
        }

        private void StatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (StatusFilterComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    _selectedStatus = null;
                }
                else
                {
                    _selectedStatus = Enum.Parse<PoStatus>(tag);
                }
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadStatistics();
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
    }

    // ViewModel for Top Suppliers
    public class TopSupplierViewModel
    {
        public int Rank { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public string TotalAmount { get; set; } = string.Empty;
    }
}
