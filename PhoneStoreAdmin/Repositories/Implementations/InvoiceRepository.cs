using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class InvoiceRepository(DataSource dataSource) : IInvoiceRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public Invoice GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM invoices WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            return null;
        }

        public IEnumerable<Invoice> GetAll()
        {
            var invoices = new List<Invoice>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                SELECT i.*, p.full_name AS person_name
                FROM invoices i
                LEFT JOIN persons p ON i.person_id = p.id
                ORDER BY i.id DESC", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                invoices.Add(MapFromReader(reader));
            }

            return invoices;
        }

        public void Insert(Invoice entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Invoice entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCustomer(int customerId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByStatus(PhoneStoreAdmin.Models.Enums.InvoiceStatus status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCreatedBy(int createdBy)
        {
            throw new NotImplementedException();
        }
        #region Helper
        private static Invoice MapFromReader(MySqlDataReader reader)
        {
            var invoice = new Invoice();

            try
            {
                invoice.Id = reader.GetInt32("id");
                invoice.PersonId = reader.IsDBNull("person_id") ? null : reader.GetInt32("person_id");
                invoice.CreatedBy = reader.GetInt32("created_by");
                invoice.InvoiceDate = reader.GetDateTime("invoice_date");

                var statusStr = reader.GetString("status");
                if (Enum.TryParse(statusStr, true, out InvoiceStatus statusEnum))
                    invoice.Status = statusEnum;
                else
                    invoice.Status = InvoiceStatus.UNPAID;

                invoice.TotalAmount = reader.GetDecimal("total_amount");
                invoice.DiscountAmount = reader.GetDecimal("discount_amount");
                invoice.FinalAmount = reader.GetDecimal("final_amount");

                var paymentStr = reader.GetString("payment_method");
                if (Enum.TryParse(paymentStr, true, out PaymentMethod paymentEnum))
                    invoice.PaymentMethod = paymentEnum;
                else
                    invoice.PaymentMethod = PaymentMethod.CASH;

                invoice.Note = reader.IsDBNull("note") ? null : reader.GetString("note");
        
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error mapping Invoice: {ex.Message}");
                throw;
            }

            return invoice;
        }

        private (string whereClause, List<MySqlParameter> parameters, string joinClause) BuildConditions(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();
            string joinClause = string.Empty;

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                joinClause = "INNER JOIN persons p ON p.id = invoices.person_id";
                conditions.Add("p.name LIKE @customerName");
                parameters.Add(new MySqlParameter("@customerName", $"%{customerName}%"));
            }

            if (customerId != null)
            {
                conditions.Add("person_id = @customerId");
                parameters.Add(new MySqlParameter("@customerId", customerId.Value));
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

            if (fromDate.HasValue)
            {
                conditions.Add("invoice_date >= @fromDate");
                parameters.Add(new MySqlParameter("@fromDate", fromDate.Value));
            }

            if (toDate.HasValue)
            {
                conditions.Add("invoice_date <= @toDate");
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

        public IEnumerable<Invoice> GetInvoicesFiltered(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 20)
        {
                var list = new List<Invoice>();
                var (whereClause, parameters, joinClause) =
                    BuildConditions(customerName, customerId, createdBy, status, fromDate, toDate, minAmount, maxAmount);

                var offset = (page - 1) * pageSize;
                var sql = $@"SELECT invoices.* FROM invoices {joinClause} {whereClause} 
                         ORDER BY id DESC LIMIT @pageSize OFFSET @offset";

                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand(sql, connection);

                foreach (var param in parameters)
                    command.Parameters.Add(param);

                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                Debug.WriteLine($"[SQL] {sql}");
                foreach (var p in parameters)
                    Debug.WriteLine($"[PARAM] {p.ParameterName} = {p.Value}");

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapFromReader(reader));
                }

                return list;
        }

        public int GetTotalRecords(string? customerName, int? customerId, int? createdBy,
            InvoiceStatus? status, DateTime? fromDate, DateTime? toDate,
            decimal? minAmount, decimal? maxAmount)
        {
            var (whereClause, parameters, joinClause) =
                BuildConditions(customerName, customerId, createdBy, status, fromDate, toDate, minAmount, maxAmount);
            var sql = $"SELECT COUNT(invoices.id) FROM invoices {joinClause} {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetTotalPages(string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(customerName, customerId, createdBy, status, fromDate, toDate, minAmount, maxAmount);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }
        #endregion
    }
}
