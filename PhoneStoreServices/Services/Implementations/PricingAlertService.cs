using System;
using System.Collections.Generic;
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
    /// Service implementation cho quản lý cảnh báo định giá
    /// </summary>
    public class PricingAlertService : IPricingAlertService
    {
        private readonly IPricingAlertRepository _pricingAlertRepository;
        private readonly IProductRepository _productRepository;
        private readonly IPricingHistoryRepository _pricingHistoryRepository;
        private readonly IDynamicPricingService _dynamicPricingService;

        public PricingAlertService(
            IPricingAlertRepository pricingAlertRepository,
            IProductRepository productRepository,
            IPricingHistoryRepository pricingHistoryRepository,
            IDynamicPricingService dynamicPricingService)
        {
            _pricingAlertRepository = pricingAlertRepository ?? throw new ArgumentNullException(nameof(pricingAlertRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _pricingHistoryRepository = pricingHistoryRepository ?? throw new ArgumentNullException(nameof(pricingHistoryRepository));
            _dynamicPricingService = dynamicPricingService ?? throw new ArgumentNullException(nameof(dynamicPricingService));
        }

        /// <inheritdoc />
        public IReadOnlyList<PricingAlertViewModel> GetPendingAlerts()
        {
            try
            {
                var alerts = _pricingAlertRepository.GetPendingAlerts();
                return alerts.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get pending alerts", ex);
                return Array.Empty<PricingAlertViewModel>();
            }
        }

        /// <inheritdoc />
        public bool ResolveAsHold(int alertId, int adminId, string? note)
        {
            try
            {
                var alert = _pricingAlertRepository.GetById(alertId);
                if (alert == null)
                {
                    Logger.Warning($"Alert {alertId} not found for ResolveAsHold");
                    return false;
                }

                if (alert.Status != AlertStatus.PENDING)
                {
                    Logger.Warning($"Alert {alertId} is already resolved with status {alert.Status}");
                    return false;
                }

                // Update alert status
                alert.Status = AlertStatus.RESOLVED_HOLD;
                alert.ResolvedBy = adminId;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedNote = note;

                _pricingAlertRepository.Update(alert);

                Logger.Info($"Alert {alertId} resolved as HOLD by admin {adminId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve alert {alertId} as hold", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public bool ResolveAsClearance(int alertId, int adminId, string? note)
        {
            try
            {
                var alert = _pricingAlertRepository.GetById(alertId);
                if (alert == null)
                {
                    Logger.Warning($"Alert {alertId} not found for ResolveAsClearance");
                    return false;
                }

                if (alert.Status != AlertStatus.PENDING)
                {
                    Logger.Warning($"Alert {alertId} is already resolved with status {alert.Status}");
                    return false;
                }

                // Get product
                var product = _productRepository.GetById(alert.ProductId);
                if (product == null)
                {
                    Logger.Error($"Product {alert.ProductId} not found for alert {alertId}");
                    return false;
                }

                var oldPrice = product.Price;

                // Change pricing mode to CLEARANCE
                product.PricingMode = PricingMode.CLEARANCE;

                // Calculate new clearance price
                var newPrice = _dynamicPricingService.CalculateClearancePrice(product.CostNifo);
                product.Price = newPrice;
                product.PriceUpdatedAt = DateTime.UtcNow;

                // Update product
                _productRepository.Update(product);

                // Log price history
                var history = new PricingHistory(
                    product.Id,
                    oldPrice,
                    newPrice,
                    product.CostFifo,
                    product.CostNifo,
                    "CLEARANCE")
                {
                    ChangedBy = adminId
                };
                _pricingHistoryRepository.Insert(history);

                // Update alert status
                alert.Status = AlertStatus.RESOLVED_CLEARANCE;
                alert.ResolvedBy = adminId;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedNote = note;

                _pricingAlertRepository.Update(alert);

                Logger.Info($"Alert {alertId} resolved as CLEARANCE by admin {adminId}. Price changed from {oldPrice} to {newPrice}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve alert {alertId} as clearance", ex);
                return false;
            }
        }

        /// <inheritdoc />
        public void CreateAlert(int productId, decimal variance, decimal costFifo, decimal costNifo, int stock)
        {
            try
            {
                var alert = new PricingAlert(productId, variance, costFifo, costNifo, stock);
                _pricingAlertRepository.Insert(alert);
                Logger.Info($"Created pricing alert for product {productId}: variance {variance}%, stock {stock}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create alert for product {productId}", ex);
                throw;
            }
        }

        /// <inheritdoc />
        public PricingAlertViewModel? GetAlertById(int alertId)
        {
            try
            {
                var alert = _pricingAlertRepository.GetById(alertId);
                return alert != null ? MapToViewModel(alert) : null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get alert {alertId}", ex);
                return null;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<PricingAlertViewModel> GetAlertsByProduct(int productId)
        {
            try
            {
                var alerts = _pricingAlertRepository.GetAlertsByProduct(productId);
                return alerts.Select(MapToViewModel).ToList();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get alerts for product {productId}", ex);
                return Array.Empty<PricingAlertViewModel>();
            }
        }

        #region Private Methods

        private PricingAlertViewModel MapToViewModel(PricingAlert alert)
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
