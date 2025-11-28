using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class ProductSerialRepository : IProductSerialRepository
    {
        private readonly DataSource _dataSource;

        public ProductSerialRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public ProductSerial GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductSerial with ID {id} not found.");
        }

        public IEnumerable<ProductSerial> GetAll()
        {
            var result = new List<ProductSerial>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(ProductSerial entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO product_serials (product_id, serial_number, imei1, imei2, batch_id, status, purchase_order_line_id, note) 
                  VALUES (@productId, @serialNumber, @imei1, @imei2, @batchId, @status, @purchaseOrderLineId, @note)",
                connection);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@serialNumber", entity.SerialNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei1", entity.Imei1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei2", entity.Imei2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@batchId", entity.BatchId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@purchaseOrderLineId", entity.PurchaseOrderLineId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        /// <summary>
        /// Insert a product serial using an existing transaction
        /// </summary>
        public void Insert(ProductSerial entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"INSERT INTO product_serials (product_id, serial_number, imei1, imei2, batch_id, status, purchase_order_line_id, note) 
      VALUES (@productId, @serialNumber, @imei1, @imei2, @batchId, @status, @purchaseOrderLineId, @note)";

            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@serialNumber", entity.SerialNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei1", entity.Imei1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei2", entity.Imei2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@batchId", entity.BatchId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@purchaseOrderLineId", entity.PurchaseOrderLineId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            command.ExecuteNonQuery();

            // Get last inserted ID using the same connection and transaction
            command.CommandText = "SELECT LAST_INSERT_ID()";
            command.Parameters.Clear();
            entity.Id = Convert.ToInt32(command.ExecuteScalar());

            command.Dispose();
        }

        public void Update(ProductSerial entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE product_serials 
                  SET product_id = @productId, serial_number = @serialNumber, imei1 = @imei1, imei2 = @imei2, 
                      batch_id = @batchId, status = @status, purchase_order_line_id = @purchaseOrderLineId, note = @note
                  WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@serialNumber", entity.SerialNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei1", entity.Imei1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei2", entity.Imei2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@batchId", entity.BatchId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@purchaseOrderLineId", entity.PurchaseOrderLineId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"ProductSerial with ID {entity.Id} not found for update.");
        }

        public void Update(ProductSerial entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(
                @"UPDATE product_serials 
                  SET product_id = @productId, serial_number = @serialNumber, imei1 = @imei1, imei2 = @imei2, 
                      batch_id = @batchId, status = @status, purchase_order_line_id = @purchaseOrderLineId, note = @note
                  WHERE id = @id",
                connection, transaction);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@serialNumber", entity.SerialNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei1", entity.Imei1 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@imei2", entity.Imei2 ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@batchId", entity.BatchId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@purchaseOrderLineId", entity.PurchaseOrderLineId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"ProductSerial with ID {entity.Id} not found for update.");
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_serials WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"ProductSerial with ID {id} not found for deletion.");
        }

        public ProductSerial GetBySerialNumber(string serialNumber)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE serial_number = @serialNumber", connection);
            command.Parameters.AddWithValue("@serialNumber", serialNumber);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductSerial with SerialNumber {serialNumber} not found.");
        }

        /// <summary>
        /// Try to get a product serial by serial number. Returns null if not found instead of throwing exception.
        /// </summary>
        public ProductSerial? TryGetBySerialNumber(string serialNumber)
        {
            try
            {
                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand("SELECT * FROM product_serials WHERE serial_number = @serialNumber", connection);
                command.Parameters.AddWithValue("@serialNumber", serialNumber);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return MapFromReader(reader);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Try to get a product serial by serial number using an existing connection. Returns null if not found.
        /// </summary>
        public ProductSerial? TryGetBySerialNumber(string serialNumber, MySqlConnection connection, MySqlTransaction transaction)
        {
            try
            {
                using var command = new MySqlCommand("SELECT * FROM product_serials WHERE serial_number = @serialNumber", connection, transaction);
                command.Parameters.AddWithValue("@serialNumber", serialNumber);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return MapFromReader(reader);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ProductSerial GetByImei1(string imei1)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE imei1 = @imei1", connection);
            command.Parameters.AddWithValue("@imei1", imei1);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductSerial with IMEI1 {imei1} not found.");
        }

        /// <summary>
        /// Try to get a product serial by IMEI1. Returns null if not found instead of throwing exception.
        /// </summary>
        public ProductSerial? TryGetByImei1(string imei1)
        {
            try
            {
                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand("SELECT * FROM product_serials WHERE imei1 = @imei1", connection);
                command.Parameters.AddWithValue("@imei1", imei1);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return MapFromReader(reader);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public ProductSerial GetByImei2(string imei2)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE imei2 = @imei2", connection);
            command.Parameters.AddWithValue("@imei2", imei2);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductSerial with IMEI2 {imei2} not found.");
        }

        /// <summary>
        /// Try to get a product serial by IMEI2. Returns null if not found instead of throwing exception.
        /// </summary>
        public ProductSerial? TryGetByImei2(string imei2)
        {
            try
            {
                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand("SELECT * FROM product_serials WHERE imei2 = @imei2", connection);
                command.Parameters.AddWithValue("@imei2", imei2);
                using var reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return MapFromReader(reader);
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public IEnumerable<ProductSerial> GetByProductId(int productId)
        {
            var result = new List<ProductSerial>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE product_id = @productId", connection);
            command.Parameters.AddWithValue("@productId", productId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public IEnumerable<ProductSerial> GetPagedByProductId(int productId, int page, int pageSize, out int totalCount, int? batchId = null)
        {
            totalCount = 0;
            if (productId <= 0)
            {
                return Enumerable.Empty<ProductSerial>();
            }

            if (page <= 0)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 20;
            }

            using var connection = _dataSource.GetConnection();
            var filters = new List<string> { "product_id = @productId" };
            if (batchId.HasValue)
            {
                filters.Add("batch_id = @batchId");
            }

            var whereClause = string.Join(" AND ", filters);

            using (var countCommand = new MySqlCommand($"SELECT COUNT(*) FROM product_serials WHERE {whereClause}", connection))
            {
                countCommand.Parameters.AddWithValue("@productId", productId);
                if (batchId.HasValue)
                {
                    countCommand.Parameters.AddWithValue("@batchId", batchId.Value);
                }

                var scalar = countCommand.ExecuteScalar();
                totalCount = scalar == null ? 0 : Convert.ToInt32(scalar);
            }

            if (totalCount == 0)
            {
                return Enumerable.Empty<ProductSerial>();
            }

            var offset = (page - 1) * pageSize;
            using var command = new MySqlCommand($"SELECT * FROM product_serials WHERE {whereClause} ORDER BY id DESC LIMIT @limit OFFSET @offset", connection);
            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@limit", pageSize);
            command.Parameters.AddWithValue("@offset", offset);
            if (batchId.HasValue)
            {
                command.Parameters.AddWithValue("@batchId", batchId.Value);
            }

            var result = new List<ProductSerial>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }

            return result;
        }

        public IEnumerable<ProductSerial> GetByStatus(SerialStatus status)
        {
            var result = new List<ProductSerial>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_serials WHERE status = @status", connection);
            command.Parameters.AddWithValue("@status", status.ToString().ToLower());
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public IDictionary<int, int> GetCountsByProductIds(IEnumerable<int> productIds)
        {
            var counts = new Dictionary<int, int>();
            if (productIds == null)
            {
                return counts;
            }

            var ids = productIds.Distinct().ToList();
            if (ids.Count == 0)
            {
                return counts;
            }

            using var connection = _dataSource.GetConnection();
            var parameterNames = ids.Select((_, index) => $"@id{index}").ToList();
            var query = $"SELECT product_id, COUNT(*) AS serial_count FROM product_serials WHERE product_id IN ({string.Join(",", parameterNames)}) GROUP BY product_id";

            using var command = new MySqlCommand(query, connection);
            for (var i = 0; i < ids.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], ids[i]);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var productId = reader.GetInt32("product_id");
                var count = reader.GetInt32("serial_count");
                counts[productId] = count;
            }

            return counts;
        }

        /// <summary>
        /// Delete all product serials associated with a purchase order
        /// </summary>
        public void DeleteByPurchaseOrderId(int purchaseOrderId, MySqlConnection connection, MySqlTransaction transaction)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"DELETE ps FROM product_serials ps
      INNER JOIN purchase_order_lines pol ON ps.purchase_order_line_id = pol.id
   WHERE pol.purchase_order_id = @poId";

            command.Parameters.AddWithValue("@poId", purchaseOrderId);
            command.ExecuteNonQuery();
            command.Dispose();
        }

        /// <summary>
        /// Update product serials status and batch for a purchase order line
        /// </summary>
        public void UpdateStatusAndBatchByLine(int lineId, SerialStatus oldStatus, SerialStatus newStatus, int batchId, string note, MySqlConnection connection, MySqlTransaction transaction)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = @"UPDATE product_serials 
       SET status = @newStatus, batch_id = @batchId, note = @note
          WHERE purchase_order_line_id = @lineId AND status = @oldStatus";

            command.Parameters.AddWithValue("@newStatus", newStatus.ToString().ToLower());
            command.Parameters.AddWithValue("@batchId", batchId);
            command.Parameters.AddWithValue("@note", note);
            command.Parameters.AddWithValue("@lineId", lineId);
            command.Parameters.AddWithValue("@oldStatus", oldStatus.ToString().ToLower());

            command.ExecuteNonQuery();
            command.Dispose();
        }

        private int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static ProductSerial MapFromReader(MySqlDataReader reader)
        {
            return new ProductSerial
            {
                Id = reader.GetInt32("id"),
                ProductId = reader.GetInt32("product_id"),
                SerialNumber = reader.IsDBNull(reader.GetOrdinal("serial_number")) ? null : reader.GetString("serial_number"),
                Imei1 = reader.IsDBNull(reader.GetOrdinal("imei1")) ? null : reader.GetString("imei1"),
                Imei2 = reader.IsDBNull(reader.GetOrdinal("imei2")) ? null : reader.GetString("imei2"),
                BatchId = reader.IsDBNull(reader.GetOrdinal("batch_id")) ? null : reader.GetInt32("batch_id"),
                Status = Enum.Parse<SerialStatus>(reader.GetString("status"), true),
                PurchaseOrderLineId = reader.IsDBNull(reader.GetOrdinal("purchase_order_line_id")) ? null : reader.GetInt32("purchase_order_line_id"),
                Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString("note")
            };
        }

        public IDictionary<int, int> GetInStockCountsByProductIds(IEnumerable<int> productIds)
        {
            var counts = new Dictionary<int, int>();
            if (productIds == null)
            {
                return counts;
            }

            var ids = new List<int>(productIds);
            if (ids.Count == 0)
            {
                return counts;
            }

            using var connection = _dataSource.GetConnection();
            var parameterNames = new List<string>();
            for (int i = 0; i < ids.Count; i++)
            {
                parameterNames.Add($"@id{i}");
            }

            // Only count serials with status 'in_stock'
            var query = $"SELECT product_id, COUNT(*) as serial_count FROM product_serials WHERE status = 'in_stock' AND product_id IN ({string.Join(",", parameterNames)}) GROUP BY product_id";

            using var command = new MySqlCommand(query, connection);
            for (int i = 0; i < ids.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], ids[i]);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var productId = reader.GetInt32("product_id");
                var count = reader.GetInt32("serial_count");
                counts[productId] = count;
            }

            return counts;
        }
    }
}
