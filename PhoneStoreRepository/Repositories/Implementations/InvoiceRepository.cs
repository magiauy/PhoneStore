using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class InvoiceRepository(DataSource dataSource) : IInvoiceRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public Invoice? GetById(int id)
        {
            using var connection = _dataSource.GetConnection();

            // Lấy hóa đơn
            using var command = new MySqlCommand(@"
                SELECT 
                    i.*, 
                    p.full_name AS customer_name, 
                    c.full_name AS creator_name,
                    pr.code AS promotion_code
                FROM invoices i
                LEFT JOIN persons p ON i.person_id = p.id
                LEFT JOIN persons c ON i.created_by = c.id
                LEFT JOIN promotion_codes pr ON i.promotion_code_id = pr.id
                WHERE i.id = @id", connection);

            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
                return null;

            var invoice = MapFromReader(reader);

            reader.Close(); // Đóng reader cũ trước khi mở truy vấn mới

            // Truy vấn các dòng chi tiết (invoice_lines)
            using var lineCmd = new MySqlCommand(@"
                SELECT 
                    il.*
                FROM invoice_lines il
                WHERE il.invoice_id = @invoiceId", connection);

            lineCmd.Parameters.AddWithValue("@invoiceId", id);

            using var lineReader = lineCmd.ExecuteReader();
            var lines = new List<InvoiceLine>();

            while (lineReader.Read())
            {
                lines.Add(new InvoiceLine
                {
                    Id = Convert.ToInt32(lineReader["id"]),
                    ProductId = Convert.ToInt32(lineReader["product_id"]),                   
                    Quantity = Convert.ToInt32(lineReader["quantity"]),
                    UnitPrice = Convert.ToDecimal(lineReader["unit_price"]),
                    DiscountPct = Convert.ToDecimal(lineReader["discount_pct"]),
                    TotalPrice = Convert.ToDecimal(lineReader["total_price"])
                });
            }

            invoice.InvoiceLines = lines; // Gán vào hóa đơn

            return invoice;
        }

        public IEnumerable<Invoice> GetAll()
        {
            var invoices = new List<Invoice>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                SELECT 
                    i.*, 
                    p.full_name AS customer_name, 
                    c.full_name AS creator_name
                FROM invoices i
                LEFT JOIN persons p ON i.person_id = p.id
                LEFT JOIN persons c ON i.created_by = c.id
                ORDER BY i.id ASC", connection);
            using var reader = command.ExecuteReader();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                Console.WriteLine(reader.GetName(i));
            }

            while (reader.Read())
            {
                invoices.Add(MapFromReader(reader));
            }

            return invoices;
        }

        public void Insert(Invoice entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                INSERT INTO invoices 
                (person_id, promotion_code_id, created_by, invoice_date, total_amount, discount_amount, final_amount, payment_method, status, note)
                VALUES
                (@personId, @promoId, @createdBy, @invoiceDate, @total, @discount, @final, @paymentMethod, @status, @note);
                SELECT LAST_INSERT_ID();", connection);

            command.Parameters.AddWithValue("@personId", entity.PersonId);
            command.Parameters.AddWithValue("@promoId", entity.PromotionCodeId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@invoiceDate", entity.InvoiceDate);
            command.Parameters.AddWithValue("@total", entity.TotalAmount);
            command.Parameters.AddWithValue("@discount", entity.DiscountAmount);
            command.Parameters.AddWithValue("@final", entity.FinalAmount);
            command.Parameters.AddWithValue("@paymentMethod", entity.PaymentMethod.ToString());
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@note", entity.Note ?? string.Empty);

            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id; // gán lại ID mới tạo
        }

        public void Insert(Invoice entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(@"
                INSERT INTO invoices 
                (person_id, promotion_code_id, created_by, invoice_date, total_amount, discount_amount, final_amount, payment_method, status, note)
                VALUES
                (@personId, @promoId, @createdBy, @invoiceDate, @total, @discount, @final, @paymentMethod, @status, @note);
                SELECT LAST_INSERT_ID();", connection, transaction);

            command.Parameters.AddWithValue("@personId", entity.PersonId);
            command.Parameters.AddWithValue("@promoId", entity.PromotionCodeId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@invoiceDate", entity.InvoiceDate);
            command.Parameters.AddWithValue("@total", entity.TotalAmount);
            command.Parameters.AddWithValue("@discount", entity.DiscountAmount);
            command.Parameters.AddWithValue("@final", entity.FinalAmount);
            command.Parameters.AddWithValue("@paymentMethod", entity.PaymentMethod.ToString());
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@note", entity.Note ?? string.Empty);

            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Update(Invoice entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                UPDATE invoices SET
                    person_id=@personId,
                    promotion_code_id=@promoId,
                    created_by=@createdBy,
                    invoice_date=@invoiceDate,
                    total_amount=@total,
                    discount_amount=@discount,
                    final_amount=@final,
                    payment_method=@paymentMethod,
                    status=@status,
                    note=@note
                WHERE id=@id", connection);

            command.Parameters.AddWithValue("@personId", entity.PersonId);
            command.Parameters.AddWithValue("@promoId", entity.PromotionCodeId ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdBy", entity.CreatedBy);
            command.Parameters.AddWithValue("@invoiceDate", entity.InvoiceDate);
            command.Parameters.AddWithValue("@total", entity.TotalAmount);
            command.Parameters.AddWithValue("@discount", entity.DiscountAmount);
            command.Parameters.AddWithValue("@final", entity.FinalAmount);
            command.Parameters.AddWithValue("@paymentMethod", entity.PaymentMethod.ToString());
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@note", entity.Note ?? string.Empty);
            command.Parameters.AddWithValue("@id", entity.Id);

            command.ExecuteNonQuery();
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

        public IEnumerable<Invoice> GetByStatus(PhoneStoreRepository.Models.Enums.InvoiceStatus status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCreatedBy(int createdBy)
        {
            throw new NotImplementedException();
        }
        private static bool HasColumn(MySqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
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
                invoice.Customer = new Person { FullName = reader["customer_name"]?.ToString() ?? "Unknow" };
                invoice.Creator = new Person { FullName = reader["creator_name"]?.ToString() ?? "System" };

                if (HasColumn(reader, "promotion_code"))
                {
                    invoice.PromotionCode = new PromotionCode { Code = reader["promotion_code"]?.ToString() ?? "Không có" };
                }
                else
                {
                    invoice.PromotionCode = new PromotionCode { Code = "Không có" };
                }
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
                conditions.Add("(person_id IN (SELECT id FROM persons WHERE full_name LIKE @customerName) " +
                   "OR created_by IN (SELECT id FROM persons WHERE full_name LIKE @customerName))");
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
                var sql = $@"
                    SELECT 
                        i.*, 
                        p.full_name AS customer_name, 
                        c.full_name AS creator_name
                    FROM invoices i
                    LEFT JOIN persons p ON i.person_id = p.id
                    LEFT JOIN persons c ON i.created_by = c.id
                    {joinClause}
                    {whereClause}
                    ORDER BY i.id DESC 
                    LIMIT {pageSize} OFFSET {offset};;
                ";
                Debug.WriteLine($"[SQL PAGE TEST] Page={page}, Offset={offset}");

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

    public class InvoiceLineRepository(DataSource dataSource) : IInvoiceLineRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public void Insert(InvoiceLine entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                INSERT INTO invoice_lines 
                (invoice_id, product_id, quantity, unit_price, discount_pct, total_price)
                VALUES 
                (@invoiceId, @productId, @quantity, @unitPrice, @discountPct, @totalPrice);
                SELECT LAST_INSERT_ID();", connection);

            command.Parameters.AddWithValue("@invoiceId", entity.InvoiceId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@unitPrice", entity.UnitPrice);
            command.Parameters.AddWithValue("@discountPct", entity.DiscountPct);
            command.Parameters.AddWithValue("@totalPrice", entity.TotalPrice);

            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Insert(InvoiceLine entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(@"
                INSERT INTO invoice_lines 
                (invoice_id, product_id, quantity, unit_price, discount_pct, total_price)
                VALUES 
                (@invoiceId, @productId, @quantity, @unitPrice, @discountPct, @totalPrice);
                SELECT LAST_INSERT_ID();", connection, transaction);

            command.Parameters.AddWithValue("@invoiceId", entity.InvoiceId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@unitPrice", entity.UnitPrice);
            command.Parameters.AddWithValue("@discountPct", entity.DiscountPct);
            command.Parameters.AddWithValue("@totalPrice", entity.TotalPrice);

            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public IEnumerable<InvoiceLine> GetByInvoiceId(int invoiceId)
        {
            var lines = new List<InvoiceLine>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                SELECT * FROM invoice_lines 
                WHERE invoice_id = @invoiceId", connection);

            command.Parameters.AddWithValue("@invoiceId", invoiceId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                lines.Add(MapFromReader(reader));
            }

            return lines;
        }

        // Các hàm Update/Delete nếu cần thiết sau này
        public void Update(InvoiceLine entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                UPDATE invoice_lines SET
                    product_id=@productId, quantity=@quantity, unit_price=@unitPrice, 
                    discount_pct=@discountPct, total_price=@totalPrice
                WHERE id=@id", connection);

            // Add parameters tương tự như Insert...
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            // ...
            command.Parameters.AddWithValue("@id", entity.Id);

            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM invoice_lines WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        // Helper map dữ liệu
        private static InvoiceLine MapFromReader(MySqlDataReader reader)
        {
            return new InvoiceLine
            {
                Id = reader.GetInt32("id"),
                InvoiceId = reader.GetInt32("invoice_id"),
                ProductId = reader.GetInt32("product_id"),
                Quantity = reader.GetInt32("quantity"),
                UnitPrice = reader.GetDecimal("unit_price"),
                DiscountPct = reader.GetDecimal("discount_pct"),
                TotalPrice = reader.GetDecimal("total_price")
            };
        }
    }
}
