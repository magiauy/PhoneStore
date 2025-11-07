using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using MySqlConnector;

namespace PhoneStoreAdmin.Repositories.Implementations
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
    }
}
