using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class BatchesRepository : IBatchesRepository
    {
        private readonly DataSource _dataSource;

        public BatchesRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public IEnumerable<Batches> GetAll()
        {
            var result = new List<Batches>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batches", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public Batches GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batches WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"Batches with ID {id} not found.");
        }

        public ICollection<Batches> GetByPurchaseOrderId(int purchaseOrderId)
        {
            var result = new List<Batches>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batches WHERE purchase_order_id = @purchaseOrderId", connection);
            command.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(Batches entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO batches (purchase_order_id, batch_code, created_at, note) 
                  VALUES (@purchaseOrderId, @batchCode, @createdAt, @note)",
                connection);
            command.Parameters.AddWithValue("@purchaseOrderId", entity.PurchaseOrderId);
            command.Parameters.AddWithValue("@batchCode", entity.BatchCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            command.ExecuteNonQuery();
            entity.id = GetLastInsertedId(connection);
        }

        public void Update(Batches entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE batches 
                  SET purchase_order_id = @purchaseOrderId, batch_code = @batchCode, created_at = @createdAt, 
                      note = @note
                  WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("@id", entity.id);
            command.Parameters.AddWithValue("@purchaseOrderId", entity.PurchaseOrderId);
            command.Parameters.AddWithValue("@batchCode", entity.BatchCode ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"Batches with ID {entity.id} not found for update.");
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM batches WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"Batches with ID {id} not found for deletion.");
        }

        public void DeleteByPurchaseOrderId(int purchaseOrderId)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM batches WHERE purchase_order_id = @purchaseOrderId", connection);
            command.Parameters.AddWithValue("@purchaseOrderId", purchaseOrderId);
            command.ExecuteNonQuery();
        }

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters, string joinClause) BuildConditions(
            int? purchaseOrderId,
            int? supplierId,
            string? batchCode,
            DateTime? fromDate,
            DateTime? toDate,
            string? note)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            string joinClause = "";

            if (supplierId.HasValue)
            {
                conditions.Add("po.supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", supplierId.Value));
                joinClause = "JOIN purchase_orders po ON batches.purchase_order_id = po.id";
            }

            if (purchaseOrderId.HasValue)
            {
                conditions.Add("purchase_order_id = @purchaseOrderId");
                parameters.Add(new MySqlParameter("@purchaseOrderId", purchaseOrderId.Value));
            }

            if (!string.IsNullOrWhiteSpace(batchCode))
            {
                conditions.Add("batch_code LIKE @batchCode");
                parameters.Add(new MySqlParameter("@batchCode", $"%{batchCode}%"));
            }

            if (fromDate.HasValue)
            {
                conditions.Add("created_at >= @fromDate");
                parameters.Add(new MySqlParameter("@fromDate", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                conditions.Add("created_at <= @toDate");
                parameters.Add(new MySqlParameter("@toDate", toDate.Value));
            }

            if (!string.IsNullOrWhiteSpace(note))
            {
                conditions.Add("note LIKE @note");
                parameters.Add(new MySqlParameter("@note", $"%{note}%"));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            return (whereClause, parameters, joinClause);
        }

        public int GetTotalRecords(int? purchaseOrderId, int? supplierId, string? batchCode, DateTime? fromDate, DateTime? toDate, string? note)
        {
            var (whereClause, parameters, joinClause) = BuildConditions(purchaseOrderId, supplierId, batchCode, fromDate, toDate, note);
            var sql = $"SELECT COUNT(*) FROM batches {joinClause} {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<Batches> GetBatchesFiltered(
            int? purchaseOrderId,
            int? supplierId,
            string? batchCode,
            DateTime? fromDate,
            DateTime? toDate,
            string? note,
            int page = 1,
            int pageSize = 20)
        {
            var list = new List<Batches>();
            var (whereClause, parameters, joinClause) = BuildConditions(purchaseOrderId, supplierId, batchCode, fromDate, toDate, note);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM batches {joinClause} {whereClause} ORDER BY batches.id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapFromReader(reader));
            }

            return list;
        }

        public int GetTotalPages(
            int? purchaseOrderId,
            int? supplierId,
            string? batchCode,
            DateTime? fromDate,
            DateTime? toDate,
            string? note,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(purchaseOrderId, supplierId, batchCode, fromDate, toDate, note);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        private int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static Batches MapFromReader(MySqlDataReader reader)
        {
            return new Batches
            {
                id = reader.GetInt32("id"),
                PurchaseOrderId = reader.GetInt32("purchase_order_id"),
                BatchCode = reader.IsDBNull("batch_code") ? null : reader.GetString("batch_code"),
                CreatedAt = reader.GetDateTime("created_at"),
                Note = reader.IsDBNull("note") ? null : reader.GetString("note")
            };
        }
    }
}