using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models.Enums;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PhoneStoreAdmin.ViewModels
{
    /// <summary>
    /// ViewModel for Pricing Dashboard Page
    /// Binds data for Summary Cards and Recent Activity list
    /// </summary>
    public class PricingDashboardViewModel : INotifyPropertyChanged
    {
        private int _totalProducts;
        private int _productsWithPriceIncrease;
        private int _productsInClearance;
        private int _pendingAlerts;
        private bool _isLoading;

        public int TotalProducts
        {
            get => _totalProducts;
            set { _totalProducts = value; OnPropertyChanged(); }
        }

        public int ProductsWithPriceIncrease
        {
            get => _productsWithPriceIncrease;
            set { _productsWithPriceIncrease = value; OnPropertyChanged(); }
        }

        public int ProductsInClearance
        {
            get => _productsInClearance;
            set { _productsInClearance = value; OnPropertyChanged(); }
        }

        public int PendingAlerts
        {
            get => _pendingAlerts;
            set { _pendingAlerts = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TrendItemViewModel> TrendItems { get; set; } = new();
        public ObservableCollection<ActivityItemViewModel> RecentActivities { get; set; } = new();

        /// <summary>
        /// Update ViewModel from dashboard data
        /// </summary>
        public void UpdateFromDashboardData(PricingDashboardData data)
        {
            TotalProducts = data.TotalProducts;
            ProductsWithPriceIncrease = data.ProductsWithPriceIncrease;
            ProductsInClearance = data.ProductsInClearance;
            PendingAlerts = data.PendingAlerts;

            TrendItems.Clear();
            foreach (var trend in data.RecentTrends.Take(10))
            {
                TrendItems.Add(new TrendItemViewModel(trend));
            }

            RecentActivities.Clear();
            // Create activity items from alerts
            foreach (var alert in data.TopAlerts.Take(5))
            {
                RecentActivities.Add(ActivityItemViewModel.FromAlert(alert));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// ViewModel for trend list items displaying FIFO vs NIFO comparison
    /// </summary>
    public class TrendItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal CostFifo { get; set; }
        public decimal CostNifo { get; set; }
        public MarketTrend MarketTrend { get; set; }
        public PricingMode PricingMode { get; set; }

        public string CostFifoDisplay => CostFifo.ToString("N0") + "đ";
        public string CostNifoDisplay => CostNifo.ToString("N0") + "đ";

        public string TrendDisplay => MarketTrend switch
        {
            MarketTrend.UP => "↗ Tăng",
            MarketTrend.DOWN => "↘ Giảm",
            _ => "→ Ổn định"
        };

        public SolidColorBrush TrendBackground => MarketTrend switch
        {
            MarketTrend.UP => new SolidColorBrush(Colors.Green) { Opacity = 0.1 },
            MarketTrend.DOWN => new SolidColorBrush(Colors.Red) { Opacity = 0.1 },
            _ => new SolidColorBrush(Colors.Gray) { Opacity = 0.1 }
        };

        public SolidColorBrush TrendForeground => MarketTrend switch
        {
            MarketTrend.UP => new SolidColorBrush(Colors.Green),
            MarketTrend.DOWN => new SolidColorBrush(Colors.Red),
            _ => new SolidColorBrush(Colors.Gray)
        };

        public TrendItemViewModel() { }

        public TrendItemViewModel(PricingTrendItem item)
        {
            ProductId = item.ProductId;
            ProductName = item.ProductName;
            ProductSku = item.ProductSku;
            CostFifo = item.CostFifo;
            CostNifo = item.CostNifo;
            MarketTrend = item.MarketTrend;
            PricingMode = item.PricingMode;
        }
    }

    /// <summary>
    /// ViewModel for recent activity items with color coding (green/red)
    /// Green: automatic price increases
    /// Red: inventory risk alerts requiring action
    /// </summary>
    public class ActivityItemViewModel
    {
        public string ActivityTitle { get; set; } = string.Empty;
        public string ActivityDescription { get; set; } = string.Empty;
        public string ActivityTime { get; set; } = string.Empty;
        public string ActivityIcon { get; set; } = "\uE8F1";
        public bool IsAlert { get; set; }

        /// <summary>
        /// Background color: red for alerts, green for price increases
        /// </summary>
        public SolidColorBrush ActivityBackground => IsAlert
            ? new SolidColorBrush(Colors.Red) { Opacity = 0.1 }
            : new SolidColorBrush(Colors.Green) { Opacity = 0.1 };

        /// <summary>
        /// Icon color: red for alerts, green for price increases
        /// </summary>
        public SolidColorBrush ActivityIconColor => IsAlert
            ? new SolidColorBrush(Colors.Red)
            : new SolidColorBrush(Colors.Green);

        /// <summary>
        /// Create activity item from pricing alert (red notification)
        /// Format: "CẢNH BÁO: [product_name] giá nhập giảm [variance]%. Tồn kho hiện tại: [quantity]. Yêu cầu hành động."
        /// </summary>
        public static ActivityItemViewModel FromAlert(PricingAlertViewModel alert)
        {
            return new ActivityItemViewModel
            {
                ActivityTitle = $"CẢNH BÁO: {alert.ProductName}",
                ActivityDescription = $"Giá nhập giảm {alert.VariancePercent:F1}%. Tồn kho: {alert.CurrentStock}. Yêu cầu hành động.",
                ActivityTime = FormatTimeAgo(alert.CreatedAt),
                ActivityIcon = "\uE7BA", // Warning icon
                IsAlert = true
            };
        }

        /// <summary>
        /// Create activity item from price increase (green notification)
        /// Format: "Đã tự động tăng giá sản phẩm [product_name] theo thị trường (+[amount])."
        /// </summary>
        public static ActivityItemViewModel FromPriceIncrease(string productName, decimal oldPrice, decimal newPrice, DateTime time)
        {
            var change = newPrice - oldPrice;
            return new ActivityItemViewModel
            {
                ActivityTitle = $"Đã tự động tăng giá: {productName}",
                ActivityDescription = $"Tăng giá theo thị trường (+{change:N0}đ)",
                ActivityTime = FormatTimeAgo(time),
                ActivityIcon = "\uE8E5", // Up arrow icon
                IsAlert = false
            };
        }

        private static string FormatTimeAgo(DateTime time)
        {
            var diff = DateTime.Now - time;
            if (diff.TotalMinutes < 1) return "Vừa xong";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} phút trước";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} giờ trước";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays} ngày trước";
            return time.ToString("dd/MM/yyyy");
        }
    }
}
