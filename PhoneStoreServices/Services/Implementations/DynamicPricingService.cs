using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PhoneStore.Services.Interfaces;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Utils;

namespace PhoneStore.Services.Implementations
{
    /// <summary>
    /// Service implementation cho Dynamic Pricing Engine
    /// </summary>
    public class DynamicPricingService : IDynamicPricingService
    {
        private readonly IProductRepository _productRepository;
        private readonly IPricingHistoryRepository _pricingHistoryRepository;
        private readonly IPricingAlertRepository _pricingAlertRepository;
        private readonly ISettingStringService _settingStringService;
        private readonly IProductSerialRepository _productSerialRepository;

        // Setting codes cho pricing configuration
        private const string SETTING_DESIRED_MARGIN = "PRICING_DESIRED_MARGIN";
        private const string SETTING_MINIMUM_MARGIN = "PRICING_MINIMUM_MARGIN";
        private const string SETTING_VARIANCE_THRESHOLD = "PRICING_VARIANCE_THRESHOLD";
        private const string SETTING_STABLE_RANGE_MAX = "PRICING_STABLE_RANGE_MAX";
        private const string SETTING_STABLE_RANGE_MIN = "PRICING_STABLE_RANGE_MIN";

        public DynamicPricingService(
            IProductRepository productRepository,
            IPricingHistoryRepository pricingHistoryRepository,
            IPricingAlertRepository pricingAlertRepository,
            ISettingStringService settingStringService,
            IProductSerialRepository productSerialRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _pricingHistoryRepository = pricingHistoryRepository ?? throw new ArgumentNullException(nameof(pricingHistoryRepository));
            _pricingAlertRepository = pricingAlertRepository ?? throw new ArgumentNullException(nameof(pricingAlertRepository));
            _settingStringService = settingStringService ?? throw new ArgumentNullException(nameof(settingStringService));
            _productSerialRepository = productSerialRepository ?? throw new ArgumentNullException(nameof(productSerialRepository));
        }

        /// <inheritdoc />
        public PricingUpdateResult UpdateNifoCost(int productId, decimal newNifoCost)
        {
            try
            {
                // Validate input
                if (newNifoCost <= 0)
                {
                    return PricingUpdateResult.CreateFailure("NIFO cost phải lớn hơn 0");
                }

                // Get product
                var product = _productRepository.GetById(productId);
                if (product == null)
                {
                    return PricingUpdateResult.CreateFailure($"Không tìm thấy sản phẩm với ID: {productId}");
                }

                var config = GetConfiguration();
                var oldPrice = product.Price;
                var oldNifo = product.CostNifo;

                // Handle new product without CostFifo - initialize CostFifo = newNifoCost (Requirement 5.1)
                if (product.CostFifo <= 0)
                {
                    product.CostFifo = newNifoCost;
                    Logger.Info($"Initialized CostFifo for product {productId}: {newNifoCost}");
                }

                // Handle new product without Price - initialize using AUTO_PROTECT formula (Requirement 5.2, 5.3)
                if (product.Price <= 0)
                {
                    var initialPrice = CalculateAutoProtectPrice(product.CostFifo, newNifoCost);
                    product.Price = initialPrice;
                    product.PriceUpdatedAt = DateTime.UtcNow;
                    LogPriceHistory(productId, 0, initialPrice, product.CostFifo, newNifoCost, "INITIAL_PRICING", null);
                    Logger.Info($"Initialized price for product {productId}: {initialPrice}");
                }

                // Update NIFO cost
                product.CostNifo = newNifoCost;

                // Calculate variance
                var variance = CalculateVariance(product.CostFifo, newNifoCost);

                // Determine market trend and action
                MarketTrend newTrend;
                string action;
                decimal? newPrice = null;

                if (variance > 0)
                {
                    // Market UP - Auto increase price
                    newTrend = MarketTrend.UP;
                    
                    if (product.PricingMode == PricingMode.AUTO_PROTECT)
                    {
                        newPrice = CalculateAutoProtectPrice(product.CostFifo, newNifoCost);
                        product.Price = newPrice.Value;
                        product.PriceUpdatedAt = DateTime.UtcNow;
                        action = "PRICE_INCREASED";

                        // Log price history
                        LogPriceHistory(productId, oldPrice, newPrice.Value, product.CostFifo, newNifoCost, "AUTO_INCREASE", null);
                        
                        Logger.Info($"Auto increased price for product {productId}: {oldPrice} -> {newPrice} (variance: {variance:P2})");
                    }
                    else
                    {
                        action = "PRICE_HELD";
                    }
                }
                else if (variance >= config.StableRangeMin && variance <= config.StableRangeMax)
                {
                    // Market STABLE - Keep current price
                    newTrend = MarketTrend.STABLE;
                    action = "PRICE_HELD";
                }
                else if (variance <= config.VarianceThreshold)
                {
                    // Market CRASH - Create alert
                    newTrend = MarketTrend.DOWN;
                    action = "ALERT_CREATED";

                    // Get current stock
                    var stockCounts = _productSerialRepository.GetInStockCountsByProductIds(new[] { productId });
                    var currentStock = stockCounts.TryGetValue(productId, out var count) ? count : 0;

                    // Create alert
                    var alert = new PricingAlert(productId, variance * 100, product.CostFifo, newNifoCost, currentStock);
                    _pricingAlertRepository.Insert(alert);

                    Logger.Warning($"Created pricing alert for product {productId}: variance {variance:P2}, stock: {currentStock}");

                    // Update product and return alert result
                    product.MarketTrend = newTrend;
                    _productRepository.Update(product);

                    return PricingUpdateResult.CreateAlertCreated(alert.Id, variance * 100, 
                        $"Cảnh báo: Giá nhập giảm {Math.Abs(variance):P2}. Tồn kho: {currentStock}");
                }
                else
                {
                    // Between stable range min and variance threshold - just update trend
                    newTrend = MarketTrend.STABLE;
                    action = "PRICE_HELD";
                }

                // Update product
                product.MarketTrend = newTrend;
                _productRepository.Update(product);

                return PricingUpdateResult.CreateSuccess(action, oldPrice, newPrice, variance * 100,
                    action == "PRICE_INCREASED" 
                        ? $"Đã tự động tăng giá từ {oldPrice:N0} lên {newPrice:N0}"
                        : "Giá được giữ nguyên");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update NIFO cost for product {productId}", ex);
                return PricingUpdateResult.CreateFailure($"Lỗi cập nhật giá: {ex.Message}");
            }
        }

        /// <inheritdoc />
        public decimal CalculateAutoProtectPrice(decimal costFifo, decimal costNifo)
        {
            var config = GetConfiguration();
            var maxCost = Math.Max(costFifo, costNifo);
            return Math.Round(maxCost * (1 + config.DesiredMargin), 2, MidpointRounding.AwayFromZero);
        }

        /// <inheritdoc />
        public decimal CalculateClearancePrice(decimal costNifo)
        {
            var config = GetConfiguration();
            return Math.Round(costNifo * (1 + config.MinimumMargin), 2, MidpointRounding.AwayFromZero);
        }

        /// <inheritdoc />
        public decimal CalculateVariance(decimal costFifo, decimal costNifo)
        {
            if (costFifo <= 0)
            {
                return 0;
            }
            return (costNifo - costFifo) / costFifo;
        }

        /// <inheritdoc />
        public PricingConfiguration GetConfiguration()
        {
            try
            {
                return new PricingConfiguration
                {
                    DesiredMargin = ParseDecimalSetting(SETTING_DESIRED_MARGIN, 0.10m),
                    MinimumMargin = ParseDecimalSetting(SETTING_MINIMUM_MARGIN, 0.05m),
                    VarianceThreshold = ParseDecimalSetting(SETTING_VARIANCE_THRESHOLD, -0.10m),
                    StableRangeMax = ParseDecimalSetting(SETTING_STABLE_RANGE_MAX, 0m),
                    StableRangeMin = ParseDecimalSetting(SETTING_STABLE_RANGE_MIN, -0.05m)
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get pricing configuration, using defaults", ex);
                return PricingConfiguration.CreateDefault();
            }
        }

        /// <inheritdoc />
        public bool UpdateConfiguration(PricingConfiguration config)
        {
            try
            {
                var success = true;
                success &= _settingStringService.SetValue(SETTING_DESIRED_MARGIN, config.DesiredMargin.ToString(CultureInfo.InvariantCulture));
                success &= _settingStringService.SetValue(SETTING_MINIMUM_MARGIN, config.MinimumMargin.ToString(CultureInfo.InvariantCulture));
                success &= _settingStringService.SetValue(SETTING_VARIANCE_THRESHOLD, config.VarianceThreshold.ToString(CultureInfo.InvariantCulture));
                success &= _settingStringService.SetValue(SETTING_STABLE_RANGE_MAX, config.StableRangeMax.ToString(CultureInfo.InvariantCulture));
                success &= _settingStringService.SetValue(SETTING_STABLE_RANGE_MIN, config.StableRangeMin.ToString(CultureInfo.InvariantCulture));

                if (success)
                {
                    Logger.Info("Updated pricing configuration successfully");
                }
                else
                {
                    Logger.Warning("Some pricing configuration settings failed to update");
                }

                return success;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to update pricing configuration", ex);
                return false;
            }
        }


        /// <inheritdoc />
        public PricingDashboardData GetDashboardData()
        {
            try
            {
                var allProducts = _productRepository.GetAll().ToList();
                var pendingAlerts = _pricingAlertRepository.GetPendingAlerts();
                var recentHistory = _pricingHistoryRepository.GetRecent(20);

                // Count products by pricing mode and trend
                var productsInClearance = allProducts.Count(p => p.PricingMode == PricingMode.CLEARANCE);
                var productsWithPriceIncrease = allProducts.Count(p => 
                    p.MarketTrend == MarketTrend.UP && 
                    p.PriceUpdatedAt.HasValue && 
                    p.PriceUpdatedAt.Value > DateTime.UtcNow.AddDays(-7));

                // Build recent trends
                var recentTrends = allProducts
                    .Where(p => p.CostFifo > 0 || p.CostNifo > 0)
                    .OrderByDescending(p => p.PriceUpdatedAt ?? p.CreatedAt)
                    .Take(10)
                    .Select(p => new PricingTrendItem
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        ProductSku = p.Sku,
                        CostFifo = p.CostFifo,
                        CostNifo = p.CostNifo,
                        CurrentPrice = p.Price,
                        MarketTrend = p.MarketTrend,
                        PricingMode = p.PricingMode,
                        PriceUpdatedAt = p.PriceUpdatedAt
                    })
                    .ToList();

                // Build top alerts
                var topAlerts = pendingAlerts
                    .Take(5)
                    .Select(a => MapAlertToViewModel(a))
                    .ToList();

                return new PricingDashboardData
                {
                    TotalProducts = allProducts.Count,
                    ProductsWithPriceIncrease = productsWithPriceIncrease,
                    ProductsInClearance = productsInClearance,
                    PendingAlerts = pendingAlerts.Count,
                    RecentTrends = recentTrends,
                    TopAlerts = topAlerts
                };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get pricing dashboard data", ex);
                return new PricingDashboardData();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<PricingHistoryViewModel> GetPriceHistory(int productId)
        {
            try
            {
                var history = _pricingHistoryRepository.GetByProduct(productId);
                var product = _productRepository.GetById(productId);

                return history.Select(h => new PricingHistoryViewModel
                {
                    Id = h.Id,
                    ProductId = h.ProductId,
                    ProductName = product?.Name ?? string.Empty,
                    ProductSku = product?.Sku ?? string.Empty,
                    OldPrice = h.OldPrice,
                    NewPrice = h.NewPrice,
                    CostFifo = h.CostFifo,
                    CostNifo = h.CostNifo,
                    ChangeReason = h.ChangeReason,
                    ChangedBy = h.ChangedBy,
                    CreatedAt = h.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get price history for product {productId}", ex);
                return Array.Empty<PricingHistoryViewModel>();
            }
        }

        /// <inheritdoc />
        public bool UpdateFifoCost(int productId, decimal newCost, int quantity)
        {
            try
            {
                // Validate input
                if (newCost <= 0)
                {
                    Logger.Warning($"Invalid newCost {newCost} for product {productId}. Cost must be greater than 0.");
                    return false;
                }

                if (quantity <= 0)
                {
                    Logger.Warning($"Invalid quantity {quantity} for product {productId}. Quantity must be greater than 0.");
                    return false;
                }

                // Get product from repository
                var product = _productRepository.GetById(productId);
                if (product == null)
                {
                    Logger.Warning($"Product {productId} not found for FIFO update");
                    return false;
                }

                var oldFifo = product.CostFifo;

                // Get current stock from ProductSerialRepository
                var stockCounts = _productSerialRepository.GetInStockCountsByProductIds(new[] { productId });
                var currentStock = stockCounts.TryGetValue(productId, out var count) ? count : 0;

                decimal newFifo;
                string reason;

                // Calculate new FIFO cost
                if (currentStock == 0 || oldFifo <= 0)
                {
                    // First time or no existing stock - set FIFO = new cost
                    newFifo = newCost;
                    reason = "INITIAL_FIFO";
                }
                else
                {
                    // Weighted average: ((OldFifo × OldStock) + (NewCost × NewQuantity)) / (OldStock + NewQuantity)
                    newFifo = ((oldFifo * currentStock) + (newCost * quantity)) / (currentStock + quantity);
                    newFifo = Math.Round(newFifo, 2, MidpointRounding.AwayFromZero);
                    reason = "FIFO_WEIGHTED_AVG";
                }

                // Update product.CostFifo and save
                product.CostFifo = newFifo;
                _productRepository.Update(product);

                // Log to PricingHistory
                LogPriceHistory(productId, product.Price, product.Price, newFifo, product.CostNifo, reason, null);

                Logger.Info($"Updated FIFO for product {productId}: {oldFifo} -> {newFifo} (reason: {reason}, stock: {currentStock}, newQty: {quantity})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update FIFO cost for product {productId}", ex);
                return false;
            }
        }

        #region Private Methods

        private decimal ParseDecimalSetting(string code, decimal defaultValue)
        {
            var value = _settingStringService.GetValue(code, defaultValue.ToString(CultureInfo.InvariantCulture));
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        private void LogPriceHistory(int productId, decimal oldPrice, decimal newPrice, decimal costFifo, decimal costNifo, string reason, int? changedBy)
        {
            try
            {
                var history = new PricingHistory(productId, oldPrice, newPrice, costFifo, costNifo, reason)
                {
                    ChangedBy = changedBy
                };
                _pricingHistoryRepository.Insert(history);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to log price history for product {productId}", ex);
            }
        }

        private PricingAlertViewModel MapAlertToViewModel(PricingAlert alert)
        {
            var product = alert.Product ?? _productRepository.GetById(alert.ProductId);
            var potentialLoss = (alert.CostFifoSnapshot - alert.CostNifoSnapshot) * alert.CurrentStock;

            return new PricingAlertViewModel
            {
                AlertId = alert.Id,
                ProductId = alert.ProductId,
                ProductName = product?.Name ?? string.Empty,
                ProductSku = product?.Sku ?? string.Empty,
                VariancePercent = alert.VariancePercent,
                CostFifo = alert.CostFifoSnapshot,
                CostNifo = alert.CostNifoSnapshot,
                CurrentStock = alert.CurrentStock,
                PotentialLoss = potentialLoss,
                Status = alert.Status,
                CreatedAt = alert.CreatedAt,
                ResolvedBy = alert.ResolvedBy,
                ResolvedAt = alert.ResolvedAt,
                ResolvedNote = alert.ResolvedNote
            };
        }

        #endregion
    }
}
