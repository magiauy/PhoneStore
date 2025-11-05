using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Helpers;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Services;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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

            if (ToDatePicker != null)
                ToDatePicker.Date = new DateTimeOffset(_toDate.Value);
            if (FromDatePicker != null)
                FromDatePicker.Date = new DateTimeOffset(_fromDate.Value);
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
                    TotalAmountText.Text = totalAmount.ToString("N0") + " ?";

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
                ShowErrorDialog("L?i", $"Không th? t?i th?ng kê: {ex.Message}");
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
                        TotalAmount = stat.TotalAmount.ToString("N0") + " ?"
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
                if (MonthlyChartGrid == null)
                    return;

                MonthlyChartGrid.Children.Clear();

                // Group by month
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

                if (!monthlyData.Any())
                {
                    var noDataText = new TextBlock
                    {
                        Text = "Không có d? li?u",
                        FontSize = 16,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    MonthlyChartGrid.Children.Add(noDataText);
                    return;
                }

                // Calculate max values for scaling
                decimal maxAmount = monthlyData.Max(m => m.Amount);
                int maxCount = monthlyData.Max(m => m.Count);

                // Chart dimensions
                double chartHeight = 250;
                double barWidth = 40;
                double spacing = 20;
                double totalWidth = monthlyData.Count * (barWidth + spacing);

                MonthlyChartGrid.Width = Math.Max(600, totalWidth);

                // Draw bars
                for (int i = 0; i < monthlyData.Count; i++)
                {
                    var data = monthlyData[i];
                    double barHeight = maxAmount > 0 ? (double)(data.Amount / maxAmount) * chartHeight : 0;
                    double x = i * (barWidth + spacing);

                    // Bar container
                    var barContainer = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Spacing = 8,
                        Margin = new Thickness(x, 0, 0, 0)
                    };

                    // Amount label
                    var amountText = new TextBlock
                    {
                        Text = (data.Amount / 1000000).ToString("N1") + "M",
                        FontSize = 11,
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229)),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    barContainer.Children.Add(amountText);

                    // Bar
                    var bar = new Border
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 79, 70, 229)),
                        CornerRadius = new CornerRadius(4, 4, 0, 0)
                    };
                    ToolTipService.SetToolTip(bar, $"{data.Count} ??n hàng\n{data.Amount:N0} ?");
                    barContainer.Children.Add(bar);

                    // Month label
                    var monthText = new TextBlock
                    {
                        Text = data.Date.ToString("MM/yy"),
                        FontSize = 11,
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    barContainer.Children.Add(monthText);

                    MonthlyChartGrid.Children.Add(barContainer);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to draw monthly chart", ex);
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
