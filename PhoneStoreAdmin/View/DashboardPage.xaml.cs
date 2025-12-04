using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using PhoneStore.Services;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.View
{
    public sealed partial class DashboardPage : Page
    {
        private readonly IDashboardService _dashboardService;
        private readonly ResourceLoader _resourceLoader;
        private int _selectedYear;
        private int _selectedMonth;
        private DashboardMonthlyData? _chartData;
        private bool _isInitializing = true;

        public DashboardPage()
        {
            this.InitializeComponent();
            
            _dashboardService = ServiceContainer.GetService<IDashboardService>();
            _resourceLoader = ResourceLoader.GetForViewIndependentUse();
            
            _selectedYear = DateTime.Now.Year;
            _selectedMonth = DateTime.Now.Month;
            
            InitializeChartFilters();
            
            this.Loaded += DashboardPage_Loaded;
        }

        private void InitializeChartFilters()
        {
            var currentYear = DateTime.Now.Year;
            for (int year = currentYear; year >= currentYear - 4; year--)
            {
                ChartYearComboBox.Items.Add(new ComboBoxItem { Content = year.ToString(), Tag = year });
            }

            ChartMonthComboBox.SelectedIndex = _selectedMonth - 1;
            ChartYearComboBox.SelectedIndex = 0;
            
            _isInitializing = false;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadChartDataAsync();
        }

        private void NewProductButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Navigate to Product creation page
        }

        private async Task LoadChartDataAsync()
        {
            try
            {
                LoadingOverlay.Visibility = Visibility.Visible;

                await Task.Run(() =>
                {
                    _chartData = _dashboardService.GetMonthlyData(_selectedYear, _selectedMonth);
                });

                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateTopLists();
                    DrawRevenueChart();
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    var errorMsg = string.Format(_resourceLoader.GetString("Dashboard_ErrorLoadingData"), ex.Message);
                    await ShowErrorDialogAsync(errorMsg);
                });
            }
        }

        private void UpdateTopLists()
        {
            if (_chartData == null) return;

            // Top Products
            if (_chartData.TopProducts.Count > 0)
            {
                TopProductsItemsControl.ItemsSource = _chartData.TopProducts;
                TopProductsItemsControl.Visibility = Visibility.Visible;
                TopProductsEmptyState.Visibility = Visibility.Collapsed;
                
                var totalRevenue = _chartData.TopProducts.Sum(p => p.TotalRevenue);
                TopProductsTotalText.Text = string.Format(_resourceLoader.GetString("Dashboard_Total"), $"{totalRevenue:N0} ₫");
            }
            else
            {
                TopProductsItemsControl.Visibility = Visibility.Collapsed;
                TopProductsEmptyState.Visibility = Visibility.Visible;
                TopProductsTotalText.Text = "";
            }

            // Top Customers
            if (_chartData.TopCustomers.Count > 0)
            {
                TopCustomersItemsControl.ItemsSource = _chartData.TopCustomers;
                TopCustomersItemsControl.Visibility = Visibility.Visible;
                TopCustomersEmptyState.Visibility = Visibility.Collapsed;
                
                var totalSpent = _chartData.TopCustomers.Sum(c => c.TotalSpent);
                TopCustomersTotalText.Text = string.Format(_resourceLoader.GetString("Dashboard_Total"), $"{totalSpent:N0} ₫");
            }
            else
            {
                TopCustomersItemsControl.Visibility = Visibility.Collapsed;
                TopCustomersEmptyState.Visibility = Visibility.Visible;
                TopCustomersTotalText.Text = "";
            }
        }

        private void DrawRevenueChart()
        {
            if (_chartData == null) return;

            RevenueChartCanvas.Children.Clear();
            
            var dailyData = _chartData.DailyRevenues;

            // Update total revenue text
            var totalRevenue = dailyData.Sum(d => d.Revenue);
            var totalOrders = dailyData.Sum(d => d.InvoiceCount);
            ChartTotalRevenueText.Text = string.Format(_resourceLoader.GetString("Dashboard_TotalRevenue"), 
                $"{totalRevenue:N0} ₫", totalOrders);

            if (dailyData.Count == 0 || dailyData.All(d => d.Revenue == 0))
            {
                RevenueChartCanvas.Visibility = Visibility.Collapsed;
                ChartEmptyState.Visibility = Visibility.Visible;
                return;
            }

            RevenueChartCanvas.Visibility = Visibility.Visible;
            ChartEmptyState.Visibility = Visibility.Collapsed;

            var maxValue = dailyData.Max(d => d.Revenue);
            if (maxValue == 0) maxValue = 1;

            // Chart dimensions
            double chartHeight = 220;
            double pointSpacing = 28;
            double leftMargin = 60;
            double topMargin = 30;
            double rightMargin = 30;

            double chartWidth = dailyData.Count * pointSpacing + leftMargin + rightMargin;
            RevenueChartCanvas.Width = Math.Max(chartWidth, 900);

            // Colors
            var lineColor = Windows.UI.Color.FromArgb(255, 76, 175, 80);
            var fillColor = Windows.UI.Color.FromArgb(40, 76, 175, 80);
            var gridColor = Windows.UI.Color.FromArgb(40, 128, 128, 128);
            var labelColor = Windows.UI.Color.FromArgb(180, 128, 128, 128);

            // Draw Y-axis labels and grid lines
            for (int i = 0; i <= 4; i++)
            {
                decimal value = maxValue * (4 - i) / 4;
                double y = topMargin + (chartHeight / 4) * i;

                var yLabel = new TextBlock
                {
                    Text = FormatShortCurrency(value),
                    FontSize = 11,
                    Foreground = new SolidColorBrush(labelColor),
                    TextAlignment = TextAlignment.Right,
                    Width = 50
                };
                Canvas.SetLeft(yLabel, 0);
                Canvas.SetTop(yLabel, y - 7);
                RevenueChartCanvas.Children.Add(yLabel);

                var gridLine = new Line
                {
                    X1 = leftMargin,
                    Y1 = y,
                    X2 = chartWidth - rightMargin,
                    Y2 = y,
                    Stroke = new SolidColorBrush(gridColor),
                    StrokeThickness = 1
                };
                RevenueChartCanvas.Children.Add(gridLine);
            }

            // Calculate points for line chart
            var points = new Windows.Foundation.Point[dailyData.Count];
            for (int i = 0; i < dailyData.Count; i++)
            {
                var data = dailyData[i];
                double x = leftMargin + i * pointSpacing + pointSpacing / 2;
                double y = topMargin + chartHeight - (maxValue > 0 ? (double)(data.Revenue / maxValue) * chartHeight : 0);
                points[i] = new Windows.Foundation.Point(x, y);
            }

            // Draw filled area under the line
            if (points.Length > 1)
            {
                var areaPath = new Path();
                var areaGeometry = new PathGeometry();
                var areaFigure = new PathFigure
                {
                    StartPoint = new Windows.Foundation.Point(points[0].X, topMargin + chartHeight),
                    IsClosed = true,
                    IsFilled = true
                };

                // Line to first data point
                areaFigure.Segments.Add(new LineSegment { Point = points[0] });

                // Line through all data points
                for (int i = 1; i < points.Length; i++)
                {
                    areaFigure.Segments.Add(new LineSegment { Point = points[i] });
                }

                // Line down to baseline and back to start
                areaFigure.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(points[points.Length - 1].X, topMargin + chartHeight) });

                areaGeometry.Figures.Add(areaFigure);
                areaPath.Data = areaGeometry;
                areaPath.Fill = new SolidColorBrush(fillColor);
                RevenueChartCanvas.Children.Add(areaPath);
            }

            // Draw line connecting points
            for (int i = 0; i < points.Length - 1; i++)
            {
                var line = new Line
                {
                    X1 = points[i].X,
                    Y1 = points[i].Y,
                    X2 = points[i + 1].X,
                    Y2 = points[i + 1].Y,
                    Stroke = new SolidColorBrush(lineColor),
                    StrokeThickness = 2.5,
                    StrokeLineJoin = PenLineJoin.Round
                };
                RevenueChartCanvas.Children.Add(line);
            }

            // Draw data points and labels
            for (int i = 0; i < dailyData.Count; i++)
            {
                var data = dailyData[i];
                var point = points[i];

                // Draw point circle
                var circle = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = new SolidColorBrush(lineColor),
                    Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                    StrokeThickness = 2
                };
                Canvas.SetLeft(circle, point.X - 4);
                Canvas.SetTop(circle, point.Y - 4);
                RevenueChartCanvas.Children.Add(circle);

                // Draw value label (only for significant values or every 5th point to avoid clutter)
                if (data.Revenue > 0 && (i % 5 == 0 || data.Revenue == maxValue))
                {
                    var valueLabel = new TextBlock
                    {
                        Text = FormatShortCurrency(data.Revenue),
                        FontSize = 9,
                        FontWeight = Microsoft.UI.Text.FontWeights.Medium,
                        Foreground = new SolidColorBrush(lineColor)
                    };
                    Canvas.SetLeft(valueLabel, point.X - 12);
                    Canvas.SetTop(valueLabel, point.Y - 18);
                    RevenueChartCanvas.Children.Add(valueLabel);
                }

                // Draw day label (every day or every 2nd day if too many)
                if (dailyData.Count <= 15 || i % 2 == 0)
                {
                    var dayLabel = new TextBlock
                    {
                        Text = data.DayDisplay,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(labelColor)
                    };
                    Canvas.SetLeft(dayLabel, point.X - 5);
                    Canvas.SetTop(dayLabel, topMargin + chartHeight + 8);
                    RevenueChartCanvas.Children.Add(dayLabel);
                }
            }
        }

        private string FormatShortCurrency(decimal value)
        {
            if (value >= 1_000_000_000)
                return $"{value / 1_000_000_000:0.#}B";
            if (value >= 1_000_000)
                return $"{value / 1_000_000:0.#}M";
            if (value >= 1_000)
                return $"{value / 1_000:0.#}K";
            return value.ToString("N0");
        }

        private void ChartMonthComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (ChartMonthComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tagStr && int.TryParse(tagStr, out int month))
            {
                _selectedMonth = month;
                _ = LoadChartDataAsync();
            }
        }

        private void ChartYearComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            
            if (ChartYearComboBox.SelectedItem is ComboBoxItem item && item.Tag is int year)
            {
                _selectedYear = year;
                _ = LoadChartDataAsync();
            }
        }

        private async void RefreshChartButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadChartDataAsync();
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("Dashboard_ErrorTitle"),
                Content = message,
                CloseButtonText = _resourceLoader.GetString("DialogClose"),
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}