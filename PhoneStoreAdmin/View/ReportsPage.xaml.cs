using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.Windows.ApplicationModel.Resources;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PhoneStore.Services;
using PhoneStore.Services.Helpers;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreAdmin.View.Controls;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Utils;
using Windows.Storage.Pickers;
using Border = Microsoft.UI.Xaml.Controls.Border;

namespace PhoneStoreAdmin.View
{
    public sealed partial class ReportsPage : Page
    {
        // ResourceLoader for localization
        private readonly ResourceLoader _resourceLoader = new();

        // Services
        private readonly IPurchaseOrderService? _purchaseOrderService;
        private readonly ISupplierService? _supplierService;
        private readonly IInvoiceService? _invoiceService;
        private readonly IProductService? _productService;

        // Data collections
        public ObservableCollection<TopSupplierViewModel> TopSuppliers { get; set; }
        public ObservableCollection<TopProductViewModel> TopProducts { get; set; }
        public ObservableCollection<TopCustomerViewModel> TopCustomers { get; set; }

        // Filter state for Purchase Orders
        private DateTime? _fromDate;
        private DateTime? _toDate;
        private PoStatus? _selectedStatus;

        // Filter state for Sales
        private DateTime? _salesFromDate;
        private DateTime? _salesToDate;
        private InvoiceStatus? _selectedSalesStatus;

        public ReportsPage()
        {
            this.InitializeComponent();
            TopSuppliers = new ObservableCollection<TopSupplierViewModel>();
            TopProducts = new ObservableCollection<TopProductViewModel>();
            TopCustomers = new ObservableCollection<TopCustomerViewModel>();
            TopSuppliersListView.ItemsSource = TopSuppliers;

            this.DataContext = this;

            // Get services from ServiceContainer
            _purchaseOrderService = ServiceContainer.GetService<IPurchaseOrderService>();
            _supplierService = ServiceContainer.GetService<ISupplierService>();
            _invoiceService = ServiceContainer.GetService<IInvoiceService>();
            _productService = ServiceContainer.GetService<IProductService>();

            // Initialize filters for Purchase Orders
            StatusFilterComboBox.SelectedIndex = 0; // All status

            // Set default date range (last 30 days)
            _toDate = DateTime.Now.Date;
            _fromDate = _toDate.Value.AddDays(-30);

            // Configure date pickers with DD/MM/YYYY format
            if (ToDatePicker != null)
            {
                ToDatePicker.Date = new DateTimeOffset(_toDate.Value);
                ToDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
            }
            if (FromDatePicker != null)
            {
                FromDatePicker.Date = new DateTimeOffset(_fromDate.Value);
                FromDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
            }

            // Initialize Sales filters
            SalesStatusFilterComboBox.SelectedIndex = 0; // All status
            _salesToDate = DateTime.Now.Date;
            _salesFromDate = _salesToDate.Value.AddDays(-30);

            if (SalesToDatePicker != null)
            {
                SalesToDatePicker.Date = new DateTimeOffset(_salesToDate.Value);
                SalesToDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
            }
            if (SalesFromDatePicker != null)
            {
                SalesFromDatePicker.Date = new DateTimeOffset(_salesFromDate.Value);
                SalesFromDatePicker.DateFormat = "{day.integer(2)}/{month.integer(2)}/{year.full}";
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
                        Text = GetLocalizedString("Reports_NoDataAvailable", "Chưa có dữ liệu"),
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

                // Chart dimensions
                double chartHeight = 250;
                double barWidth = 40;
                // Increase spacing so columns are farther apart
                double spacing = 80;
                double totalWidth = monthlyData.Count * (barWidth + spacing);

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
            // Refresh based on current tab
            if (ReportsPivot.SelectedIndex == 0)
            {
                LoadStatistics();
            }
            else if (ReportsPivot.SelectedIndex == 1)
            {
                LoadSalesStatistics();
            }
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

        #region Sales Statistics

        private void LoadSalesStatistics()
        {
            try
            {
                if (_invoiceService == null || _productService == null)
                    return;

                // Get filtered invoices
                var result = _invoiceService.GetInvoicesFiltered(
                    null, // customerName
                    null, // customerId
                    null, // createdBy
                    _selectedSalesStatus,
                    _salesFromDate,
                    _salesToDate,
                    null, // minAmount
                    null, // maxAmount
                    1, // page
                    10000 // large pageSize to get all records
                );

                if (result == null || result.Invoices == null)
                    return;

                var invoices = result.Invoices.ToList();

                // Calculate summary statistics
                int totalInvoices = invoices.Count;
                decimal totalRevenue = invoices.Sum(i => i.FinalAmount);
                int paidInvoices = invoices.Count(i => i.Status == InvoiceStatus.PAID || i.Status == InvoiceStatus.COMPLETED);
                int pendingInvoices = invoices.Count(i => i.Status == InvoiceStatus.PENDING || i.Status == InvoiceStatus.UNPAID);

                // Update UI
                if (TotalInvoicesText != null)
                    TotalInvoicesText.Text = totalInvoices.ToString("N0");

                if (TotalRevenueText != null)
                    TotalRevenueText.Text = totalRevenue.ToString("N0");

                if (PaidInvoicesText != null)
                    PaidInvoicesText.Text = paidInvoices.ToString("N0");

                if (PendingInvoicesText != null)
                    PendingInvoicesText.Text = pendingInvoices.ToString("N0");

                // Load Top Products using service method
                LoadTopProducts();

                // Load Top Customers
                LoadTopCustomers(invoices);

                // Draw Sales Monthly Chart
                DrawSalesMonthlyChart(invoices);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load sales statistics", ex);
                ShowErrorDialog("Lỗi", $"Không thể tải thống kê bán hàng: {ex.Message}");
            }
        }

        private void LoadTopProducts()
        {
            try
            {
                if (_invoiceService == null)
                    return;

                // Use service method to get top selling products
                var topProductStats = _invoiceService.GetTopSellingProducts(
                    _salesFromDate,
                    _salesToDate,
                    _selectedSalesStatus,
                    5
                );

                TopProducts.Clear();
                TopProductsListView.ItemsSource = TopProducts;
                int rank = 1;
                foreach (var stat in topProductStats)
                {
                    TopProducts.Add(new TopProductViewModel
                    {
                        Rank = rank++,
                        ProductName = stat.ProductName,
                        QuantitySold = stat.QuantitySold,
                        TotalRevenue = stat.TotalRevenue.ToString("N0")
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load top products", ex);
            }
        }

        private void LoadTopCustomers(List<InvoiceViewModel> invoices)
        {
            try
            {
                // Group by customer
                var customerStats = invoices
                    .GroupBy(i => new { i.PersonId, i.CustomerName })
                    .Select(g => new
                    {
                        CustomerId = g.Key.PersonId,
                        CustomerName = g.Key.CustomerName,
                        InvoiceCount = g.Count(),
                        TotalSpent = g.Sum(i => i.FinalAmount)
                    })
                    .OrderByDescending(c => c.TotalSpent)
                    .Take(5)
                    .ToList();

                TopCustomers.Clear();
                TopCustomersListView.ItemsSource = TopCustomers;
                int rank = 1;
                foreach (var stat in customerStats)
                {
                    TopCustomers.Add(new TopCustomerViewModel
                    {
                        Rank = rank++,
                        CustomerName = stat.CustomerName,
                        InvoiceCount = stat.InvoiceCount,
                        TotalSpent = stat.TotalSpent.ToString("N0")
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load top customers", ex);
            }
        }

        private void DrawSalesMonthlyChart(List<InvoiceViewModel> invoices)
        {
            try
            {
                if (SalesMonthlyChartGrid == null) return;

                SalesMonthlyChartGrid.Children.Clear();

                // 1. Prepare Data
                var monthlyData = invoices
                    .GroupBy(i => new { Year = i.InvoiceDate.Year, Month = i.InvoiceDate.Month })
                    .Select(g => new
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Count = g.Count(),
                        Amount = g.Sum(i => i.FinalAmount)
                    })
                    .OrderBy(m => m.Date)
                    .ToList();

                // 2. Handle No Data
                if (!monthlyData.Any())
                {
                    var noDataText = new TextBlock
                    {
                        Text = GetLocalizedString("Reports_NoDataAvailable", "Chưa có dữ liệu"),
                        Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
                        Foreground = Application.Current.Resources["TextFillColorTertiaryBrush"] as Brush,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    SalesMonthlyChartGrid.Children.Add(noDataText);
                    return;
                }

                // 3. Calculation Constants
                decimal maxAmount = monthlyData.Max(m => m.Amount);
                if (maxAmount == 0) maxAmount = 1;

                double chartHeight = 260;
                double barWidth = 32;

                // 4. Create Main Container
                var chartContainer = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 24,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Padding = new Thickness(0, 0, 0, 10)
                };

                // 5. Draw Bars
                foreach (var data in monthlyData)
                {
                    double heightPercentage = (double)(data.Amount / maxAmount);
                    double barHeight = Math.Max(4, heightPercentage * (chartHeight - 50));

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
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)), // Green color
                        HorizontalAlignment = HorizontalAlignment.Center,
                        HorizontalTextAlignment = TextAlignment.Center,
                        Opacity = heightPercentage > 0.1 ? 1 : 0
                    };

                    // B. The Bar (Middle)
                    var barBorder = new Border
                    {
                        Width = barWidth,
                        Height = barHeight,
                        Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80)), // Green color
                        CornerRadius = new CornerRadius(4),
                        Opacity = 0.9
                    };

                    // Tooltip
                    var toolTipContent = new StackPanel { Spacing = 4 };
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"Tháng {data.Date:MM/yyyy}",
                        FontWeight = Microsoft.UI.Text.FontWeights.Bold
                    });
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"{data.Count} hóa đơn",
                        Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as Brush
                    });
                    toolTipContent.Children.Add(new TextBlock
                    {
                        Text = $"{data.Amount:N0} ₫",
                        Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 76, 175, 80))
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

                    columnStack.Children.Add(amountText);
                    columnStack.Children.Add(barBorder);
                    columnStack.Children.Add(monthText);

                    chartContainer.Children.Add(columnStack);
                }

                SalesMonthlyChartGrid.Children.Add(chartContainer);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to draw sales chart", ex);
            }
        }

        private void SalesDateFilter_Changed(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (sender == SalesFromDatePicker && args.NewDate.HasValue)
            {
                _salesFromDate = args.NewDate.Value.DateTime.Date;
            }
            else if (sender == SalesToDatePicker && args.NewDate.HasValue)
            {
                _salesToDate = args.NewDate.Value.DateTime.Date.AddDays(1).AddSeconds(-1);
            }
        }

        private void SalesStatusFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (SalesStatusFilterComboBox?.SelectedItem is ComboBoxItem item && item.Tag is string tag)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    _selectedSalesStatus = null;
                }
                else
                {
                    _selectedSalesStatus = Enum.Parse<InvoiceStatus>(tag);
                }
            }
        }

        private void ApplySalesFilter_Click(object sender, RoutedEventArgs e)
        {
            LoadSalesStatistics();
        }

        #endregion

        private void ReportsPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Load data when switching tabs
            if (ReportsPivot.SelectedIndex == 0)
            {
                LoadStatistics();
            }
            else if (ReportsPivot.SelectedIndex == 1)
            {
                LoadSalesStatistics();
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

        #region Export Excel

        private async void ExportPurchaseOrderExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_purchaseOrderService == null)
                    return;

                // Get filtered data
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
                    10000 // large pageSize
                );

                if (result == null || result.PurchaseOrders == null || !result.PurchaseOrders.Any())
                {
                    ShowErrorDialog(GetLocalizedString("Reports_ExportError", "Lỗi"), 
                        GetLocalizedString("Reports_NoDataToExport", "Không có dữ liệu để xuất"));
                    return;
                }

                var orders = result.PurchaseOrders.ToList();

                // Create FileSavePicker
                var savePicker = new FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
                savePicker.SuggestedFileName = $"BaoCaoNhapHang_{DateTime.Now:yyyyMMdd_HHmmss}";

                var file = await savePicker.PickSaveFileAsync();
                if (file == null) return;

                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(GetLocalizedString("Reports_PurchaseOrdersTab", "Đơn nhập hàng"));

                    // Title
                    worksheet.Cells["A1"].Value = GetLocalizedString("Reports_PurchaseOrdersReportTitle", "BÁO CÁO ĐƠN NHẬP HÀNG");
                    worksheet.Cells["A1:H1"].Merge = true;
                    worksheet.Cells["A1"].Style.Font.Size = 16;
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Date range
                    var dateRangeText = $"{(_fromDate?.ToString("dd/MM/yyyy") ?? "...")} - {(_toDate?.ToString("dd/MM/yyyy") ?? "...")}";
                    worksheet.Cells["A2"].Value = dateRangeText;
                    worksheet.Cells["A2:H2"].Merge = true;
                    worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Headers
                    int headerRow = 4;
                    worksheet.Cells[headerRow, 1].Value = "STT";
                    worksheet.Cells[headerRow, 2].Value = GetLocalizedString("Reports_OrderIdHeader", "Mã đơn");
                    worksheet.Cells[headerRow, 3].Value = GetLocalizedString("Reports_SupplierHeader", "Nhà cung cấp");
                    worksheet.Cells[headerRow, 4].Value = GetLocalizedString("Reports_OrderDateHeader", "Ngày đặt");
                    worksheet.Cells[headerRow, 5].Value = GetLocalizedString("Reports_StatusHeader", "Trạng thái");
                    worksheet.Cells[headerRow, 6].Value = GetLocalizedString("Reports_TotalAmountHeader", "Tổng tiền");
                    worksheet.Cells[headerRow, 7].Value = GetLocalizedString("Reports_NoteHeader", "Ghi chú");

                    // Header style
                    using (var range = worksheet.Cells[headerRow, 1, headerRow, 7])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(70, 130, 180));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Data rows
                    int row = headerRow + 1;
                    int stt = 1;
                    decimal totalSum = 0;

                    foreach (var order in orders)
                    {
                        worksheet.Cells[row, 1].Value = stt++;
                        worksheet.Cells[row, 2].Value = $"PO-{order.Id:D6}";
                        worksheet.Cells[row, 3].Value = order.SupplierName;
                        worksheet.Cells[row, 4].Value = order.OrderDate.ToString("dd/MM/yyyy");
                        worksheet.Cells[row, 5].Value = GetStatusDisplayName(order.Status);
                        worksheet.Cells[row, 6].Value = order.TotalAmount;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 7].Value = order.Note ?? "";

                        totalSum += order.TotalAmount;
                        row++;
                    }

                    // Summary row
                    worksheet.Cells[row + 1, 1].Value = GetLocalizedString("Reports_TotalLabel", "TỔNG CỘNG:");
                    worksheet.Cells[row + 1, 1, row + 1, 5].Merge = true;
                    worksheet.Cells[row + 1, 1].Style.Font.Bold = true;
                    worksheet.Cells[row + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells[row + 1, 6].Value = totalSum;
                    worksheet.Cells[row + 1, 6].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row + 1, 6].Style.Font.Bold = true;

                    // Auto fit columns
                    worksheet.Cells.AutoFitColumns();
                    worksheet.Column(1).Width = 6;

                    // Save file
                    var stream = await file.OpenStreamForWriteAsync();
                    stream.SetLength(0);
                    await package.SaveAsAsync(stream);
                    stream.Close();
                }

                // Show success message
                await ShowSuccessDialog(
                    GetLocalizedString("Reports_ExportSuccess", "Thành công"),
                    GetLocalizedString("Reports_ExportSuccessMessage", "Xuất báo cáo Excel thành công!"));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to export purchase orders to Excel", ex);
                ShowErrorDialog(GetLocalizedString("Reports_ExportError", "Lỗi"), 
                    $"{GetLocalizedString("Reports_ExportErrorMessage", "Không thể xuất báo cáo")}: {ex.Message}");
            }
        }

        private async void ExportSalesExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_invoiceService == null)
                    return;

                // Get filtered data
                var result = _invoiceService.GetInvoicesFiltered(
                    null, // customerName
                    null, // customerId
                    null, // createdBy
                    _selectedSalesStatus,
                    _salesFromDate,
                    _salesToDate,
                    null, // minAmount
                    null, // maxAmount
                    1, // page
                    10000 // large pageSize
                );

                if (result == null || result.Invoices == null || !result.Invoices.Any())
                {
                    ShowErrorDialog(GetLocalizedString("Reports_ExportError", "Lỗi"), 
                        GetLocalizedString("Reports_NoDataToExport", "Không có dữ liệu để xuất"));
                    return;
                }

                var invoices = result.Invoices.ToList();

                // Create FileSavePicker
                var savePicker = new FileSavePicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(((App)Application.Current).CurrentWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hwnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Excel Files", new List<string>() { ".xlsx" });
                savePicker.SuggestedFileName = $"BaoCaoBanHang_{DateTime.Now:yyyyMMdd_HHmmss}";

                var file = await savePicker.PickSaveFileAsync();
                if (file == null) return;

                // Set EPPlus license context
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add(GetLocalizedString("Reports_SalesTab", "Hóa đơn bán hàng"));

                    // Title
                    worksheet.Cells["A1"].Value = GetLocalizedString("Reports_SalesReportTitle", "BÁO CÁO BÁN HÀNG");
                    worksheet.Cells["A1:H1"].Merge = true;
                    worksheet.Cells["A1"].Style.Font.Size = 16;
                    worksheet.Cells["A1"].Style.Font.Bold = true;
                    worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Date range
                    var dateRangeText = $"{(_salesFromDate?.ToString("dd/MM/yyyy") ?? "...")} - {(_salesToDate?.ToString("dd/MM/yyyy") ?? "...")}";
                    worksheet.Cells["A2"].Value = dateRangeText;
                    worksheet.Cells["A2:H2"].Merge = true;
                    worksheet.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    // Headers
                    int headerRow = 4;
                    worksheet.Cells[headerRow, 1].Value = "STT";
                    worksheet.Cells[headerRow, 2].Value = GetLocalizedString("Reports_InvoiceIdHeader", "Mã hóa đơn");
                    worksheet.Cells[headerRow, 3].Value = GetLocalizedString("Reports_CustomerHeader", "Khách hàng");
                    worksheet.Cells[headerRow, 4].Value = GetLocalizedString("Reports_InvoiceDateHeader", "Ngày tạo");
                    worksheet.Cells[headerRow, 5].Value = GetLocalizedString("Reports_PaymentMethodHeader", "Thanh toán");
                    worksheet.Cells[headerRow, 6].Value = GetLocalizedString("Reports_StatusHeader", "Trạng thái");
                    worksheet.Cells[headerRow, 7].Value = GetLocalizedString("Reports_TotalAmountHeader", "Tổng tiền");
                    worksheet.Cells[headerRow, 8].Value = GetLocalizedString("Reports_CreatedByHeader", "Người tạo");

                    // Header style
                    using (var range = worksheet.Cells[headerRow, 1, headerRow, 8])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(46, 125, 50));
                        range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    // Data rows
                    int row = headerRow + 1;
                    int stt = 1;
                    decimal totalSum = 0;

                    foreach (var invoice in invoices)
                    {
                        worksheet.Cells[row, 1].Value = stt++;
                        worksheet.Cells[row, 2].Value = $"INV-{invoice.Id:D6}";
                        worksheet.Cells[row, 3].Value = invoice.CustomerName;
                        worksheet.Cells[row, 4].Value = invoice.InvoiceDate.ToString("dd/MM/yyyy HH:mm");
                        worksheet.Cells[row, 5].Value = GetPaymentMethodDisplayName(invoice.PaymentMethod);
                        worksheet.Cells[row, 6].Value = GetInvoiceStatusDisplayName(invoice.Status);
                        worksheet.Cells[row, 7].Value = invoice.FinalAmount;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                        worksheet.Cells[row, 8].Value = invoice.CreatedByName;

                        totalSum += invoice.FinalAmount;
                        row++;
                    }

                    // Summary row
                    worksheet.Cells[row + 1, 1].Value = GetLocalizedString("Reports_TotalLabel", "TỔNG CỘNG:");
                    worksheet.Cells[row + 1, 1, row + 1, 6].Merge = true;
                    worksheet.Cells[row + 1, 1].Style.Font.Bold = true;
                    worksheet.Cells[row + 1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                    worksheet.Cells[row + 1, 7].Value = totalSum;
                    worksheet.Cells[row + 1, 7].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[row + 1, 7].Style.Font.Bold = true;

                    // Auto fit columns
                    worksheet.Cells.AutoFitColumns();
                    worksheet.Column(1).Width = 6;

                    // Save file
                    var stream = await file.OpenStreamForWriteAsync();
                    stream.SetLength(0);
                    await package.SaveAsAsync(stream);
                    stream.Close();
                }

                // Show success message
                await ShowSuccessDialog(
                    GetLocalizedString("Reports_ExportSuccess", "Thành công"),
                    GetLocalizedString("Reports_ExportSuccessMessage", "Xuất báo cáo Excel thành công!"));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to export sales to Excel", ex);
                ShowErrorDialog(GetLocalizedString("Reports_ExportError", "Lỗi"), 
                    $"{GetLocalizedString("Reports_ExportErrorMessage", "Không thể xuất báo cáo")}: {ex.Message}");
            }
        }

        private string GetStatusDisplayName(PoStatus status)
        {
            return status switch
            {
                PoStatus.DRAFT => GetLocalizedString("Reports_DraftStatus", "Nháp"),
                PoStatus.RECEIVED => GetLocalizedString("Reports_ReceivedStatus", "Đã nhận"),
                PoStatus.CANCELLED => GetLocalizedString("Reports_CancelledStatus", "Đã hủy"),
                _ => status.ToString()
            };
        }

        private string GetInvoiceStatusDisplayName(InvoiceStatus status)
        {
            return status switch
            {
                InvoiceStatus.PENDING => GetLocalizedString("Reports_PendingStatus", "Chờ xử lý"),
                InvoiceStatus.UNPAID => GetLocalizedString("Reports_UnpaidStatus", "Chưa thanh toán"),
                InvoiceStatus.PAID => GetLocalizedString("Reports_PaidStatus", "Đã thanh toán"),
                InvoiceStatus.COMPLETED => GetLocalizedString("Reports_CompletedStatus", "Hoàn thành"),
                InvoiceStatus.CANCELLED => GetLocalizedString("Reports_CancelledStatus", "Đã hủy"),
                InvoiceStatus.DELIVERING => GetLocalizedString("Reports_DeliveringStatus", "Đang giao"),
                InvoiceStatus.REFUNDED => GetLocalizedString("Reports_RefundedStatus", "Đã hoàn tiền"),
                _ => status.ToString()
            };
        }

        private string GetPaymentMethodDisplayName(PaymentMethod method)
        {
            return method switch
            {
                PaymentMethod.CASH => GetLocalizedString("Reports_CashPayment", "Tiền mặt"),
                PaymentMethod.CARD => GetLocalizedString("Reports_CreditCardPayment", "Thẻ tín dụng"),
                PaymentMethod.BANK => GetLocalizedString("Reports_BankTransferPayment", "Chuyển khoản"),
                PaymentMethod.EWALLET => GetLocalizedString("Reports_EWalletPayment", "Ví điện tử"),
                _ => method.ToString()
            };
        }

        private async Task ShowSuccessDialog(string title, string message)
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

        /// <summary>
        /// Safely get localized string, returns fallback if not found
        /// </summary>
        private string GetLocalizedString(string key, string fallback)
        {
            try
            {
                var value = _resourceLoader.GetString(key);
                return string.IsNullOrEmpty(value) ? fallback : value;
            }
            catch
            {
                return fallback;
            }
        }

        #endregion
    }

    // ViewModel for Top Suppliers
    public class TopSupplierViewModel
    {
        public int Rank { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public string TotalAmount { get; set; } = string.Empty;
    }

    // ViewModel for Top Products
    public class TopProductViewModel
    {
        public int Rank { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public string TotalRevenue { get; set; } = string.Empty;
    }

    // ViewModel for Top Customers
    public class TopCustomerViewModel
    {
        public int Rank { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public string TotalSpent { get; set; } = string.Empty;
    }
}
