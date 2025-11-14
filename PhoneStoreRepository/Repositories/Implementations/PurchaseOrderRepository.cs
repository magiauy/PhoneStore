using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using PhoneStoreRepository.Models;
using System.Diagnostics;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private readonly DataSource _dataSource;

        public PurchaseOrderRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        public PurchaseOrder GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM purchase_orders WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"PurchaseOrder with ID {id} not found.");
        }

        public IEnumerable<PurchaseOrder> GetAll()
        {
            var result = new List<PurchaseOrder>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM purchase_orders ORDER BY id DESC LIMIT 20 OFFSET 0", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(PurchaseOrder entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO purchase_orders (supplier_id, created_by, order_date, status, total_amount, note) 
                  VALUES (@supplierId, @createdBy, @orderDate, @status, @totalAmount, @note)",
                connection);

            command.Parameters.AddWithValue("@supplierId", entity.SupplierId);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@orderDate", entity.OrderDate);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@totalAmount", entity.TotalAmount);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Insert(PurchaseOrder entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(
                @"INSERT INTO purchase_orders (supplier_id, created_by, order_date, status, total_amount, note) 
                  VALUES (@supplierId, @createdBy, @orderDate, @status, @totalAmount, @note)",
                connection, transaction);

            command.Parameters.AddWithValue("@supplierId", entity.SupplierId);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@orderDate", entity.OrderDate);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@totalAmount", entity.TotalAmount);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
            
            // Get last inserted ID with transaction
            using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
            entity.Id = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        public void Update(PurchaseOrder entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE purchase_orders 
                  SET supplier_id = @supplierId, created_by = @createdBy, order_date = @orderDate, 
                      status = @status, total_amount = @totalAmount, note = @note
                  WHERE id = @id",   // sửa Id -> id
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@supplierId", entity.SupplierId);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@orderDate", entity.OrderDate);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@totalAmount", entity.TotalAmount);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PurchaseOrder with ID {entity.Id} not found for update.");
        }

        public void Update(PurchaseOrder entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(
                @"UPDATE purchase_orders 
                  SET supplier_id = @supplierId, created_by = @createdBy, order_date = @orderDate, 
                      status = @status, total_amount = @totalAmount, note = @note
                  WHERE id = @id",
                connection, transaction);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@supplierId", entity.SupplierId);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@orderDate", entity.OrderDate);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@totalAmount", entity.TotalAmount);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PurchaseOrder with ID {entity.Id} not found for update.");
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM purchase_orders WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PurchaseOrder with ID {id} not found for deletion.");
        }

        private int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
        
        private int GetLastInsertedId(MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters, string joinClause) BuildConditions(
            string? supplierName,
            int? supplierId,  // Đổi từ int? supplierId
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            string joinClause = string.Empty;

            if (!string.IsNullOrWhiteSpace(supplierName))
            {
                joinClause = "INNER JOIN suppliers s ON s.id = purchase_orders.supplier_id";
                conditions.Add("s.name LIKE @supplierName");
                parameters.Add(new MySqlParameter("@supplierName", $"%{supplierName}%"));
            }

            if (supplierId != null)
            {
                conditions.Add("supplier_id = @supplierId");
                parameters.Add(new MySqlParameter("@supplierId", supplierId.Value));
            }

            if (createdBy.HasValue)
            {
                conditions.Add("created_by = @createdBy");
                parameters.Add(new MySqlParameter("@createdBy", createdBy.Value));
            }

            if (status.HasValue)
            {
                conditions.Add("status = @status");
                parameters.Add(new MySqlParameter("@status", status.Value.ToString()));
            }

            if (!string.IsNullOrWhiteSpace(note))
            {
                conditions.Add("note LIKE @note");
                parameters.Add(new MySqlParameter("@note", $"%{note}%"));
            }

            if (fromDate.HasValue)
            {
                conditions.Add("order_date >= @fromDate");
                parameters.Add(new MySqlParameter("@fromDate", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                conditions.Add("order_date <= @toDate");
                parameters.Add(new MySqlParameter("@toDate", toDate.Value));
            }

            if (minAmount.HasValue)
            {
                conditions.Add("total_amount >= @minAmount");
                parameters.Add(new MySqlParameter("@minAmount", minAmount.Value));
            }

            if (maxAmount.HasValue)
            {
                conditions.Add("total_amount <= @maxAmount");
                parameters.Add(new MySqlParameter("@maxAmount", maxAmount.Value));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            return (whereClause, parameters, joinClause);
        }

        public int GetTotalRecords(string? supplierName, int? supplierId, int? createdBy, PoStatus? status, string? note,
            DateTime? fromDate, DateTime? toDate, decimal? minAmount, decimal? maxAmount)
        {
            var (whereClause, parameters, joinClause) = BuildConditions(supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount);
            var sql = $"SELECT COUNT(purchase_orders.id) FROM purchase_orders {joinClause} {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<PurchaseOrder> GetPurchaseOrdersFiltered(
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
            int pageSize = 20)
        {
            var list = new List<PurchaseOrder>();
            var (whereClause, parameters, joinClause) = BuildConditions(supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT purchase_orders.* FROM purchase_orders {joinClause} {whereClause}  ORDER BY Id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            Debug.WriteLine($"[SQL] {sql}");
            foreach (var p in parameters)
                Debug.WriteLine($"[PARAM] {p.ParameterName} = {p.Value}");

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
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(supplierName, supplierId, createdBy, status, note, fromDate, toDate, minAmount, maxAmount);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static PurchaseOrder MapFromReader(MySqlDataReader reader)
        {
            var po = new PurchaseOrder();

            try
            {
                System.Diagnostics.Debug.WriteLine("Thông tin debug ở đây");

                po.Id = reader.IsDBNull(reader.GetOrdinal("id")) ? 0 : reader.GetInt32("id");
                po.SupplierId = reader.IsDBNull(reader.GetOrdinal("supplier_id")) ? 0 : reader.GetInt32("supplier_id");
                po.CreatedBy = reader.IsDBNull(reader.GetOrdinal("created_by")) ? 0 : reader.GetInt32("created_by");
                po.OrderDate = reader.IsDBNull(reader.GetOrdinal("order_date")) ? DateTime.MinValue : reader.GetDateTime("order_date");

                if (!reader.IsDBNull(reader.GetOrdinal("status")))
                {
                    var statusStr = reader.GetString("status");
                    Debug.WriteLine($"status raw value: {statusStr} ({statusStr?.GetType()})");
                    if (Enum.TryParse<PoStatus>(statusStr, true, out var statusEnum))
                        po.Status = statusEnum;
                    else
                        po.Status = PoStatus.DRAFT;
                }
                else
                {
                    po.Status = PoStatus.DRAFT;
                }

                if (!reader.IsDBNull(reader.GetOrdinal("total_amount")))
                {
                    var value = reader.GetValue(reader.GetOrdinal("total_amount"));
                    Debug.WriteLine($"total_amount raw value: {value} ({value?.GetType()})");

                    try
                    {
                        po.TotalAmount = Convert.ToDecimal(value);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error converting total_amount: {ex.Message}");
                        po.TotalAmount = 0m;
                    }
                }
                else
                {
                    po.TotalAmount = 0m;
                }

                po.Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString("note");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping PurchaseOrder: {ex.Message}");
                throw;
            }

            return po;
        }


        #endregion
    }
}