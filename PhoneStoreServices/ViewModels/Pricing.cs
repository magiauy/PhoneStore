using System;
using System.Collections.Generic;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStore.Services.ViewModels
{
    /// <summary>
    /// Kết quả của việc cập nhật giá
    /// </summary>
    public class PricingUpdateResult
    {
        public bool Success { get; set; }
        
        /// <summary>
        /// Hành động đã thực hiện: PRICE_INCREASED, PRICE_HELD, ALERT_CREATED
        /// </summary>
        public string Action { get; set; } = string.Empty;
        
        public decimal? OldPrice { get; set; }
        public decimal? NewPrice { get; set; }
        public decimal? VariancePercent { get; set; }
        public int? AlertId { get; set; }
        public string? Message { get; set; }

        public static PricingUpdateResult CreateSuccess(string action, decimal? oldPrice = null, decimal? newPrice = null, decimal? variance = null, string? message = null)
        {
            return new PricingUpdateResult
            {
                Success = true,
                Action = action,
                OldPrice = oldPrice,
                NewPrice = newPrice,
                VariancePercent = variance,
                Message = message
            };
        }

        public static PricingUpdateResult CreateAlertCreated(int alertId, decimal variance, string? message = null)
        {
            return new PricingUpdateResult
            {
                Success = true,
                Action = "ALERT_CREATED",
                AlertId = alertId,
                VariancePercent = variance,
                Message = message
            };
        }

        public static PricingUpdateResult CreateFailure(string message)
        {
            return new PricingUpdateResult
            {
                Success = false,
                Action = "FAILED",
                Message = message
            };
        }
    }

    /// <summary>
    /// Cấu hình pricing từ SettingString
    /// </summary>
    public class PricingConfiguration
    {
        /// <summary>
        /// Biên lợi nhuận mong muốn (mặc định 10%)
        /// </summary>
        public decimal DesiredMargin { get; set; } = 0.10m;

        /// <summary>
        /// Biên lợi nhuận tối thiểu khi xả hàng (mặc định 5%)
        /// </summary>
        public decimal MinimumMargin { get; set; } = 0.05m;

        /// <summary>
        /// Ngưỡng chênh lệch giá kích hoạt cảnh báo (mặc định -10%)
        /// </summary>
        public decimal VarianceThreshold { get; set; } = -0.10m;

        /// <summary>
        /// Ngưỡng trên của vùng ổn định (mặc định 0%)
        /// </summary>
        public decimal StableRangeMax { get; set; } = 0m;

        /// <summary>
        /// Ngưỡng dưới của vùng ổn định (mặc định -5%)
        /// </summary>
        public decimal StableRangeMin { get; set; } = -0.05m;

        public static PricingConfiguration CreateDefault()
        {
            return new PricingConfiguration
            {
                DesiredMargin = 0.10m,
                MinimumMargin = 0.05m,
                VarianceThreshold = -0.10m,
                StableRangeMax = 0m,
                StableRangeMin = -0.05m
            };
        }
    }

    /// <summary>
    /// Dữ liệu dashboard pricing
    /// </summary>
    public class PricingDashboardData
    {
        public int TotalProducts { get; set; }
        public int ProductsWithPriceIncrease { get; set; }
        public int ProductsInClearance { get; set; }
        public int PendingAlerts { get; set; }
        public IReadOnlyList<PricingTrendItem> RecentTrends { get; set; } = Array.Empty<PricingTrendItem>();
        public IReadOnlyList<PricingAlertViewModel> TopAlerts { get; set; } = Array.Empty<PricingAlertViewModel>();
    }

    /// <summary>
    /// Item xu hướng giá cho biểu đồ
    /// </summary>
    public class PricingTrendItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal CostFifo { get; set; }
        public decimal CostNifo { get; set; }
        public decimal CurrentPrice { get; set; }
        public MarketTrend MarketTrend { get; set; }
        public PricingMode PricingMode { get; set; }
        public DateTime? PriceUpdatedAt { get; set; }
    }

    /// <summary>
    /// ViewModel cho cảnh báo pricing
    /// </summary>
    public class PricingAlertViewModel
    {
        public int AlertId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal VariancePercent { get; set; }
        public decimal CostFifo { get; set; }
        public decimal CostNifo { get; set; }
        public int CurrentStock { get; set; }
        
        /// <summary>
        /// Tổn thất tiềm năng: (FIFO - NIFO) * Stock
        /// </summary>
        public decimal PotentialLoss { get; set; }
        
        public AlertStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? ResolvedBy { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string? ResolvedNote { get; set; }

        /// <summary>
        /// Hiển thị variance dạng phần trăm
        /// </summary>
        public string VarianceDisplay => $"{VariancePercent:F2}%";

        /// <summary>
        /// Hiển thị trạng thái
        /// </summary>
        public string StatusDisplay => Status switch
        {
            AlertStatus.PENDING => "Chờ xử lý",
            AlertStatus.RESOLVED_HOLD => "Đã giữ giá",
            AlertStatus.RESOLVED_CLEARANCE => "Đã kích hoạt xả hàng",
            _ => "Không xác định"
        };
    }

    /// <summary>
    /// ViewModel cho lịch sử biến động giá
    /// </summary>
    public class PricingHistoryViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        public decimal CostFifo { get; set; }
        public decimal CostNifo { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
        public int? ChangedBy { get; set; }
        public string? ChangedByName { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Chênh lệch giá
        /// </summary>
        public decimal PriceChange => NewPrice - OldPrice;

        /// <summary>
        /// Phần trăm thay đổi giá
        /// </summary>
        public decimal PriceChangePercent => OldPrice > 0 ? (PriceChange / OldPrice) * 100 : 0;

        /// <summary>
        /// Hiển thị lý do thay đổi
        /// </summary>
        public string ChangeReasonDisplay => ChangeReason switch
        {
            "AUTO_INCREASE" => "Tự động tăng giá",
            "CLEARANCE" => "Kích hoạt xả hàng",
            "MANUAL" => "Thay đổi thủ công",
            _ => ChangeReason
        };

        /// <summary>
        /// Màu sắc hiển thị (green cho tăng, red cho giảm)
        /// </summary>
        public string ColorCode => PriceChange >= 0 ? "green" : "red";
    }
}
