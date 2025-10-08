using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IPurchaseOrderLineRepository _lineRepository;
        private readonly IBatchesRepository _batchesRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly IProductRepository _productRepository;

        public PurchaseOrderService(
            IPurchaseOrderRepository poRepository, 
            IPurchaseOrderLineRepository lineRepository,
            IBatchesRepository batchesRepository,
            IBatchProductRepository batchProductRepository,
            IProductRepository productRepository)
        {
            _poRepository = poRepository ?? throw new ArgumentNullException(nameof(poRepository));
            _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
            _batchesRepository = batchesRepository ?? throw new ArgumentNullException(nameof(batchesRepository));
            _batchProductRepository = batchProductRepository ?? throw new ArgumentNullException(nameof(batchProductRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        }

        public PurchaseOrder? GetById(int id)
        {
            try
            {
                Logger.Info($"Getting PurchaseOrder by ID: {id}");
                var po = _poRepository.GetById(id);
                if (po != null)
                {
                    po.PurchaseOrderLines = _lineRepository.GetByPurchaseOrderId(id);
                }
                return po;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get PurchaseOrder by ID: {id}", ex);
                return null;
            }
        }

        public void Insert(PurchaseOrder po)
        {
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Inserting PurchaseOrder for Supplier {po.SupplierId}");
                
                // Insert purchase order
                _poRepository.Insert(po);
                
                // Insert purchase order lines
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line);
                }

                // Create batch automatically
                CreateBatchForPurchaseOrder(po);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert PurchaseOrder", ex);
                throw;
            }
        }

        private void CreateBatchForPurchaseOrder(PurchaseOrder po)
        {
            try
            {
                // Generate batch code: BATCH-POID-YYYYMMDD-HHMMSS
                var batchCode = $"BATCH-{po.Id}-{DateTime.Now:yyyyMMdd-HHmmss}";

                // Create batch
                var batch = new Batches
                {
                    PurchaseOrderId = po.Id,
                    BatchCode = batchCode,
                    CreatedAt = DateTime.Now,
                    Note = po.Note
                };

                _batchesRepository.Insert(batch);
                Logger.Info($"Created batch {batchCode} for PurchaseOrder {po.Id}");

                // Create batch products for each line
                foreach (var line in po.PurchaseOrderLines)
                {
                    var product = _productRepository.GetById(line.ProductId);
                    
                    var batchProduct = new BatchProduct
                    {
                        BatchId = batch.id,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        CostPrice = line.UnitCost,
                        SellingPrice = product.Price
                    };

                    _batchProductRepository.Insert(batchProduct);
                    Logger.Info($"Created batch product for Product {line.ProductId} in Batch {batch.id}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create batch for PurchaseOrder {po.Id}", ex);
                throw;
            }
        }

        public void Update(PurchaseOrder po)
        {
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Updating PurchaseOrder ID {po.Id}");
                _poRepository.Update(po);
                _lineRepository.DeleteByPurchaseOrderId(po.Id);
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update PurchaseOrder {po.Id}", ex);
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                Logger.Info($"Deleting PurchaseOrder ID {id}");
                
                // Delete batches and batch products first
                var batches = _batchesRepository.GetByPurchaseOrderId(id);
                foreach (var batch in batches)
                {
                    _batchProductRepository.DeleteByBatchId(batch.id);
                }
                _batchesRepository.DeleteByPurchaseOrderId(id);
                
                // Delete purchase order lines
                _lineRepository.DeleteByPurchaseOrderId(id);
                
                // Delete purchase order
                _poRepository.Delete(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete PurchaseOrder {id}", ex);
                throw;
            }
        }

        public int CountAll()
        {
            try
            {
                return _poRepository.GetTotalRecords(null, null, null, null, null, null, null, null, null);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to count all PurchaseOrders", ex);
                return 0;
            }
        }

        #region Filter + Pagination
        public PurchaseOrderResult GetPurchaseOrdersFiltered(
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var orders = _poRepository.GetPurchaseOrdersFiltered(
                    supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount, page, pageSize);

                var totalRecords = _poRepository.GetTotalRecords(
                    supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount);

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                return new PurchaseOrderResult(
                    orders,
                    new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered PurchaseOrders", ex);
                return new PurchaseOrderResult(new PurchaseOrder[0], new InfoTable(0, 0));
            }
        }
        #endregion

        #region Business logic
        public void MarkAsReceived(int id)
        {
            var po = GetById(id);
            if (po != null && po.Status == PoStatus.DRAFT)
            {
                po.Status = PoStatus.RECEIVED;
                _poRepository.Update(po);
            }
        }

        public void CancelOrder(int id)
        {
            var po = GetById(id);
            if (po != null && po.Status != PoStatus.CANCELLED)
            {
                po.Status = PoStatus.CANCELLED;
                _poRepository.Update(po);
            }
        }
        #endregion
    }
}