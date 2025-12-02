using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
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
    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly IPurchaseOrderRepository _poRepository;
        private readonly IPurchaseOrderLineRepository _lineRepository;
        private readonly IBatchesRepository _batchesRepository;
        private readonly IBatchProductRepository _batchProductRepository;
        private readonly IProductRepository _productRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        private readonly DataSource _dataSource;

        public PurchaseOrderService(
              IPurchaseOrderRepository poRepository,
                IPurchaseOrderLineRepository lineRepository,
                IBatchesRepository batchesRepository,
            IBatchProductRepository batchProductRepository,
        IProductRepository productRepository,
                IProductSerialRepository productSerialRepository,
       DataSource dataSource)
        {
            _poRepository = poRepository ?? throw new ArgumentNullException(nameof(poRepository));
            _lineRepository = lineRepository ?? throw new ArgumentNullException(nameof(lineRepository));
            _batchesRepository = batchesRepository ?? throw new ArgumentNullException(nameof(batchesRepository));
            _batchProductRepository = batchProductRepository ?? throw new ArgumentNullException(nameof(batchProductRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _productSerialRepository = productSerialRepository ?? throw new ArgumentNullException(nameof(productSerialRepository));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
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

        public void Insert(PurchaseOrder po, Dictionary<int, List<ProductSerial>>? serialsByLineId = null)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Inserting PurchaseOrder for Supplier {po.SupplierId}");

                // Insert purchase order with transaction
                _poRepository.Insert(po, connection, transaction);

                // Insert purchase order lines and their serials with transaction
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line, connection, transaction);

                    // Check if product is serial tracked
                    var product = _productRepository.GetById(line.ProductId);
                    if (product.IsSerialTracked)
                    {
                        // Get serials for this line (if provided by user)
                        List<ProductSerial>? serials = null;

                        // Try to find serials by temporary line identifier or index
                        if (serialsByLineId != null)
                        {
                            // If serials were mapped by line index (0-based)
                            var lineIndex = po.PurchaseOrderLines.ToList().IndexOf(line);
                            if (serialsByLineId.TryGetValue(lineIndex, out var mappedSerials))
                            {
                                serials = mappedSerials;
                            }
                        }

                        if (serials != null && serials.Count > 0)
                        {
                            // User provided serials - insert them
                            Logger.Info($"Creating {serials.Count} user-provided product serials for Product {product.Name} (ID: {product.Id})");

                            foreach (var serial in serials)
                            {
                                // Validate
                                if (string.IsNullOrWhiteSpace(serial.SerialNumber))
                                {
                                    throw new Exception($"Serial number is required for product {product.Name}");
                                }

                                // Set properties
                                serial.ProductId = line.ProductId;
                                serial.PurchaseOrderLineId = line.Id;
                                serial.BatchId = null; // No batch yet
                                serial.Status = SerialStatus.RESERVED; // RESERVED until received
                                if (string.IsNullOrWhiteSpace(serial.Note))
                                {
                                    serial.Note = $"Created with PO {po.Id} - awaiting receipt";
                                }

                                _productSerialRepository.Insert(serial, connection, transaction);
                            }

                            Logger.Info($"Created {serials.Count} product serials for Product {product.Name}");
                        }
                        else
                        {
                            // No serials provided - create placeholder serials
                            Logger.Info($"Creating {line.Quantity} placeholder serials for Product {product.Name} (ID: {product.Id})");

                            for (int i = 0; i < line.Quantity; i++)
                            {
                                var productSerial = new ProductSerial
                                {
                                    ProductId = line.ProductId,
                                    SerialNumber = $"TEMP-PO{po.Id:D6}-P{product.Id:D4}-{(i + 1):D3}", // Temporary serial
                                    Imei1 = null,
                                    Imei2 = null,
                                    BatchId = null,
                                    Status = SerialStatus.RESERVED,
                                    PurchaseOrderLineId = line.Id,
                                    Note = $"Placeholder - Update serial/IMEI before receiving"
                                };

                                _productSerialRepository.Insert(productSerial, connection, transaction);
                            }

                            Logger.Info($"Created {line.Quantity} placeholder serials for Product {product.Name}");
                        }
                    }
                }

                transaction.Commit();
                Logger.Info($"PurchaseOrder {po.Id} created successfully with DRAFT status and product serials");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error("Failed to insert PurchaseOrder - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }

        public void Update(PurchaseOrder po)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                po.TotalAmount = po.PurchaseOrderLines.Sum(l => l.TotalCost);
                Logger.Info($"Updating PurchaseOrder ID {po.Id}");

                // Update purchase order with transaction
                _poRepository.Update(po, connection, transaction);

                // Delete old lines and insert new ones with transaction
                _lineRepository.DeleteByPurchaseOrderId(po.Id, connection, transaction);
                foreach (var line in po.PurchaseOrderLines)
                {
                    line.PurchaseOrderId = po.Id;
                    _lineRepository.Insert(line, connection, transaction);
                }

                transaction.Commit();
                Logger.Info($"PurchaseOrder {po.Id} updated successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to update PurchaseOrder {po.Id} - transaction rolled back", ex);
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
                Logger.Info($"Deleting PurchaseOrder ID {id}");

                // Delete product serials associated with this PO first
                _productSerialRepository.DeleteByPurchaseOrderId(id, connection, transaction);
                Logger.Info($"Deleted product serials for PurchaseOrder {id}");

                // Delete batches and batch products with transaction
                var batches = _batchesRepository.GetByPurchaseOrderId(id);
                foreach (var batch in batches)
                {
                    _batchProductRepository.DeleteByBatchId(batch.id, connection, transaction);
                }
                _batchesRepository.DeleteByPurchaseOrderId(id, connection, transaction);

                // Delete purchase order lines with transaction
                _lineRepository.DeleteByPurchaseOrderId(id, connection, transaction);

                // Delete purchase order with transaction
                using (var command = new MySqlConnector.MySqlCommand("DELETE FROM purchase_orders WHERE id = @id", connection, transaction))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                Logger.Info($"PurchaseOrder {id} deleted successfully");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to delete PurchaseOrder {id} - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
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
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                var po = GetById(id);
                if (po == null)
                {
                    throw new Exception("Purchase order not found");
                }

                if (po.Status != PoStatus.DRAFT)
                {
                    throw new Exception("Only DRAFT purchase orders can be marked as received");
                }

                Logger.Info($"Marking PurchaseOrder {id} as RECEIVED and creating batch");

                // Generate batch code
                var batchCode = $"BATCH-{po.Id}-{DateTime.Now:yyyyMMdd-HHmmss}";

                // Create batch with transaction
                var batch = new Batches
                {
                    PurchaseOrderId = po.Id,
                    BatchCode = batchCode,
                    CreatedAt = DateTime.Now,
                    Note = po.Note
                };

                _batchesRepository.Insert(batch, connection, transaction);
                Logger.Info($"Created batch {batchCode} for PurchaseOrder {po.Id}");

                // Create batch products and update product serials
                foreach (var line in po.PurchaseOrderLines)
                {
                    var product = _productRepository.GetById(line.ProductId);

                    // Tính giá bán dựa trên giá nhập và biên độ lợi nhuận
                    var sellingPrice = line.UnitCost * (1 + (decimal)line.ProfitMargin);

                    var batchProduct = new BatchProduct
                    {
                        BatchId = batch.id,
                        ProductId = line.ProductId,
                        Quantity = line.Quantity,
                        CostPrice = line.UnitCost,
                        SellingPrice = sellingPrice,
                        ProfitMargin = line.ProfitMargin
                    };

                    _batchProductRepository.Insert(batchProduct, connection, transaction);
                    Logger.Info($"Created batch product for Product {line.ProductId} in Batch {batch.id} with profit margin {line.ProfitMargin:P0}");

                    // Update RESERVED serials to IN_STOCK and link to batch
                    if (product.IsSerialTracked)
                    {
                        Logger.Info($"Updating RESERVED serials to IN_STOCK for Product {product.Name}");

                        // Update serials using repository method
                        _productSerialRepository.UpdateStatusAndBatchByLine(
                          line.Id, 
                        SerialStatus.RESERVED, 
                      SerialStatus.IN_STOCK, 
                       batch.id,
                            $"Received in batch {batchCode}",
                           connection, 
                      transaction);

                       Logger.Info($"Updated serials from RESERVED to IN_STOCK for line {line.Id}");
                    }
                }

                // Update purchase order status with transaction
                po.Status = PoStatus.RECEIVED;
                _poRepository.Update(po, connection, transaction);

                transaction.Commit();
                Logger.Info($"Purchase order {id} marked as RECEIVED successfully with serials updated to IN_STOCK");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to mark purchase order {id} as received - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }

        public void CancelOrder(int id)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                var po = GetById(id);
                if (po == null)
                {
                    throw new Exception("Purchase order not found");
                }

                if (po.Status == PoStatus.CANCELLED)
                {
                    throw new Exception("Purchase order is already cancelled");
                }

                Logger.Info($"Cancelling PurchaseOrder {id}");

                // Delete product serials associated with this PO
                _productSerialRepository.DeleteByPurchaseOrderId(id, connection, transaction);
                Logger.Info($"Deleted product serials for cancelled PurchaseOrder {id}");

                // Delete associated batches and batch products with transaction
                var batches = _batchesRepository.GetByPurchaseOrderId(id);
                foreach (var batch in batches)
                {
                    _batchProductRepository.DeleteByBatchId(batch.id, connection, transaction);
                }
                _batchesRepository.DeleteByPurchaseOrderId(id, connection, transaction);

                // Update purchase order status with transaction
                po.Status = PoStatus.CANCELLED;
                _poRepository.Update(po, connection, transaction);

                transaction.Commit();
                Logger.Info($"Purchase order {id} cancelled successfully with serials deleted");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to cancel purchase order {id} - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }

        /// <summary>
        /// Add product serials to a purchase order line (nhập thủ công serial/IMEI)
        /// </summary>
        public void AddProductSerials(int purchaseOrderId, int purchaseOrderLineId, List<ProductSerial> serials)
        {
            var (connection, transaction) = _dataSource.BeginTransaction();
            try
            {
                var po = GetById(purchaseOrderId);
                if (po == null)
                {
                    throw new Exception("Purchase order not found");
                }

                if (po.Status != PoStatus.DRAFT)
                {
                    throw new Exception("Can only add serials to DRAFT purchase orders");
                }

                Logger.Info($"Adding {serials.Count} product serials to PurchaseOrderLine {purchaseOrderLineId}");

                foreach (var serial in serials)
                {
                    // Validate serial info
                    if (string.IsNullOrWhiteSpace(serial.SerialNumber))
                    {
                        throw new Exception("Serial number is required");
                    }

                    // Set common properties
                    serial.PurchaseOrderLineId = purchaseOrderLineId;
                    serial.BatchId = null; // Chưa có batch
                    serial.Status = SerialStatus.RESERVED; // RESERVED until received
                    serial.Note = $"Added to PO {purchaseOrderId} - awaiting receipt";

                    _productSerialRepository.Insert(serial, connection, transaction);
                }

                transaction.Commit();
                Logger.Info($"Successfully added {serials.Count} product serials to PurchaseOrderLine {purchaseOrderLineId}");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Logger.Error($"Failed to add product serials - transaction rolled back", ex);
                throw;
            }
            finally
            {
                transaction.Dispose();
                connection.Dispose();
            }
        }
        #endregion
    }
}