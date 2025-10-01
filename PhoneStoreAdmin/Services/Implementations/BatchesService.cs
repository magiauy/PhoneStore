using PhoneStoreAdmin.Models;
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
    public class BatchesService : IBatchesService
    {
        private readonly IBatchesRepository _batchesRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISupplierRepository _supplierRepository;

        public BatchesService(IBatchesRepository batchesRepository, IBatchProductRepository batchProductRepository, IPurchaseOrderRepository purchaseOrderRepository, ISupplierRepository supplierRepository)
        {
            _batchesRepository = batchesRepository ?? throw new ArgumentNullException(nameof(batchesRepository));
            _batchProductRepository = batchProductRepository ?? throw new ArgumentNullException(nameof(batchProductRepository));
            _purchaseOrderRepository = purchaseOrderRepository ?? throw new ArgumentNullException(nameof(purchaseOrderRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        }

        public Batches? GetById(int id)
        {
            try
            {
                Logger.Info($"Getting Batches by ID: {id}");
                var batch = _batchesRepository.GetById(id);
                if (batch != null)
                {
                    batch.BatchProducts = _batchProductRepository.GetByBatchId(id).ToList();
                }
                return batch;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Batches by ID: {id}", ex);
                return null;
            }
        }

        public void Insert(Batches batch)
        {
            try
            {
                Logger.Info($"Inserting Batches for PurchaseOrder {batch.PurchaseOrderId}");
                _batchesRepository.Insert(batch);
                foreach (var product in batch.BatchProducts)
                {
                    product.BatchId = batch.id;
                    _batchProductRepository.Insert(product);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert Batches", ex);
                throw;
            }
        }

        public void Update(Batches batch)
        {
            try
            {
                Logger.Info($"Updating Batches ID {batch.id}");
                _batchesRepository.Update(batch);
                _batchProductRepository.DeleteByBatchId(batch.id);
                foreach (var product in batch.BatchProducts)
                {
                    product.BatchId = batch.id;
                    _batchProductRepository.Insert(product);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update Batches {batch.id}", ex);
                throw;
            }
        }

        public void Delete(int id)
        {
            try
            {
                Logger.Info($"Deleting Batches ID {id}");
                _batchProductRepository.DeleteByBatchId(id);
                _batchesRepository.Delete(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete Batches {id}", ex);
                throw;
            }
        }

        #region Filter + Pagination
        public BatchesResult GetBatchesFiltered(
            int? purchaseOrderId,
            int? supplierId,
            string? batchCode,
            DateTime? fromDate,
            DateTime? toDate,
            string? note,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var batches = _batchesRepository.GetBatchesFiltered(
                    purchaseOrderId, supplierId, batchCode, fromDate, toDate, note, page, pageSize);

                var totalRecords = _batchesRepository.GetTotalRecords(
                    purchaseOrderId, supplierId, batchCode, fromDate, toDate, note);

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

                foreach(var batch in batches)
                {
                    batch.BatchProducts = _batchProductRepository.GetByBatchId(batch.id).ToList();
                    batch.PurchaseOrder = _purchaseOrderRepository.GetById(batch.PurchaseOrderId);
                    batch.PurchaseOrder.Supplier = _supplierRepository.GetById(batch.PurchaseOrder.SupplierId);
                }

                return new BatchesResult(
                    batches,
                    new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered Batches", ex);
                return new BatchesResult(new Batches[0], new InfoTable(0, 0));
            }
        }
        #endregion

        #region Business logic
        // Có thể thêm business logic nếu cần, ví dụ: MarkAsProcessed
        public void MarkAsProcessed(int id)
        {
            var batch = GetById(id);
            if (batch != null)
            {
                // Logic xử lý nếu cần, ví dụ cập nhật status nếu có field status
                Update(batch);
            }
        }
        #endregion
    }
}