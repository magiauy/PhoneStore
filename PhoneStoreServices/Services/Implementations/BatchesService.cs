using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Data;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.Implementations
{
    public class BatchesService : IBatchesService
    {
        private readonly IBatchesRepository _batchesRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly IPurchaseOrderRepository _purchaseOrderRepository;
        private readonly ISupplierRepository _supplierRepository;
        private readonly DataSource _dataSource;
        private readonly IDynamicPricingService _dynamicPricingService;

        public BatchesService(
            IBatchesRepository batchesRepository,
            IBatchProductRepository batchProductRepository,
            IPurchaseOrderRepository purchaseOrderRepository,
            ISupplierRepository supplierRepository,
            DataSource dataSource,
            IDynamicPricingService dynamicPricingService)
        {
            _batchesRepository = batchesRepository ?? throw new ArgumentNullException(nameof(batchesRepository));
            _batchProductRepository = batchProductRepository ?? throw new ArgumentNullException(nameof(batchProductRepository));
            _purchaseOrderRepository = purchaseOrderRepository ?? throw new ArgumentNullException(nameof(purchaseOrderRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _dynamicPricingService = dynamicPricingService ?? throw new ArgumentNullException(nameof(dynamicPricingService));
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
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                Logger.Info($"Inserting Batches for PurchaseOrder {batch.PurchaseOrderId}");

                // Insert batch with transaction
                _batchesRepository.Insert(batch, connection, transaction);

                // Insert batch products with transaction
                foreach (var product in batch.BatchProducts)
                {
                    product.BatchId = batch.id;
                    _batchProductRepository.Insert(product, connection, transaction);
                }

                transaction.Commit();
                Logger.Info($"Batch {batch.id} created successfully");

                // Trigger dynamic pricing update for each product in the batch
                // This is done after commit to ensure data consistency
                TriggerPricingUpdates(batch.BatchProducts);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error("Failed to insert Batches - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }

        /// <summary>
        /// Trigger dynamic pricing updates for batch products
        /// Updates FIFO cost first (weighted average), then NIFO cost
        /// Order of operations is important: FIFO must be updated before NIFO for correct variance calculation
        /// </summary>
        private void TriggerPricingUpdates(IEnumerable<BatchProduct> batchProducts)
        {
            foreach (var batchProduct in batchProducts)
            {
                try
                {
                    // Step 1: Update FIFO cost first (weighted average calculation)
                    // This must be done BEFORE UpdateNifoCost to ensure correct variance calculation
                    var fifoResult = _dynamicPricingService.UpdateFifoCost(
                        batchProduct.ProductId, 
                        batchProduct.CostPrice, 
                        batchProduct.Quantity);
                    
                    if (fifoResult)
                    {
                        Logger.Info($"FIFO cost updated for product {batchProduct.ProductId}: cost={batchProduct.CostPrice}, qty={batchProduct.Quantity}");
                    }
                    else
                    {
                        Logger.Warning($"FIFO cost update failed for product {batchProduct.ProductId}");
                    }

                    // Step 2: Update NIFO cost and trigger pricing workflow
                    // This will calculate variance against the updated FIFO and create alerts if needed
                    var nifoResult = _dynamicPricingService.UpdateNifoCost(batchProduct.ProductId, batchProduct.CostPrice);
                    
                    if (nifoResult.Success)
                    {
                        Logger.Info($"Pricing update for product {batchProduct.ProductId}: {nifoResult.Action} - {nifoResult.Message}");
                    }
                    else
                    {
                        Logger.Warning($"Pricing update failed for product {batchProduct.ProductId}: {nifoResult.Message}");
                    }
                }
                catch (Exception ex)
                {
                    // Log error but don't fail the batch creation
                    Logger.Error($"Failed to trigger pricing update for product {batchProduct.ProductId}", ex);
                }
            }
        }

        public void Update(Batches batch)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                Logger.Info($"Updating Batches ID {batch.id}");

                // Update batch with transaction (cần thêm method này trong BatchesRepository)
                // Hiện tại Update không có overload với transaction, vì vậy sẽ dùng method thông thường
                // nhưng gọi trong transaction context
                using (var command = new MySqlCommand(
@"UPDATE batches 
 SET purchase_order_id = @purchaseOrderId, batch_code = @batchCode, created_at = @createdAt, 
     note = @note
WHERE id = @id",
                    connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", batch.id);
                    command.Parameters.AddWithValue("@purchaseOrderId", batch.PurchaseOrderId);
                    command.Parameters.AddWithValue("@batchCode", batch.BatchCode ?? (object)System.DBNull.Value);
                    command.Parameters.AddWithValue("@createdAt", batch.CreatedAt);
                    command.Parameters.AddWithValue("@note", batch.Note ?? (object)System.DBNull.Value);
                    command.ExecuteNonQuery();
                }

                // Delete old batch products and insert new ones with transaction
                _batchProductRepository.DeleteByBatchId(batch.id, connection, transaction);
                foreach (var product in batch.BatchProducts)
                {
                    product.BatchId = batch.id;
                    _batchProductRepository.Insert(product, connection, transaction);
                }

                transaction.Commit();
                Logger.Info($"Batch {batch.id} updated successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to update Batches {batch.id} - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }

        public void Delete(int id)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                Logger.Info($"Deleting Batches ID {id}");

                // Delete batch products with transaction
                _batchProductRepository.DeleteByBatchId(id, connection, transaction);

                // Delete batch with transaction
                using (var command = new MySqlCommand("DELETE FROM batches WHERE id = @id", connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                Logger.Info($"Batch {id} deleted successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to delete Batches {id} - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
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