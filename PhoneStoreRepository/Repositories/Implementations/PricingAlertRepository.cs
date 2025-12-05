using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation cho quản lý cảnh báo định giá
    /// </summary>
    public class PricingAlertRepository : IPricingAlertRepository
    {
        private readonly DataSource _dataSource;

        public PricingAlertRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public PricingAlert? GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM pricing_alert WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            return null;
        }

        public IReadOnlyList<PricingAlert> GetPendingAlerts()
        {
            var result = new List<PricingAlert>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM pricing_alert WHERE status = @status ORDER BY created_at DESC",
                connection);
            command.Parameters.AddWithValue("@status", (int)AlertStatus.PENDING);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }


        public IReadOnlyList<PricingAlert> GetAlertsByProduct(int productId)
        {
            var result = new List<PricingAlert>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM pricing_alert WHERE product_id = @productId ORDER BY created_at DESC",
                connection);
            command.Parameters.AddWithValue("@productId", productId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(PricingAlert alert)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO pricing_alert 
                  (product_id, alert_type, variance_percent, cost_fifo_snapshot, cost_nifo_snapshot, 
                   current_stock, status, resolved_by, resolved_at, resolved_note, created_at) 
                  VALUES 
                  (@productId, @alertType, @variancePercent, @costFifoSnapshot, @costNifoSnapshot, 
                   @currentStock, @status, @resolvedBy, @resolvedAt, @resolvedNote, @createdAt)",
                connection);
            AddParameters(command, alert);
            command.ExecuteNonQuery();
            alert.Id = GetLastInsertedId(connection);
        }

        public void Update(PricingAlert alert)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE pricing_alert SET 
                  product_id = @productId, 
                  alert_type = @alertType,
                  variance_percent = @variancePercent, 
                  cost_fifo_snapshot = @costFifoSnapshot, 
                  cost_nifo_snapshot = @costNifoSnapshot, 
                  current_stock = @currentStock, 
                  status = @status, 
                  resolved_by = @resolvedBy, 
                  resolved_at = @resolvedAt, 
                  resolved_note = @resolvedNote
                  WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("@id", alert.Id);
            AddParameters(command, alert);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"PricingAlert with ID {alert.Id} not found for update.");
        }

        private static void AddParameters(MySqlCommand command, PricingAlert alert)
        {
            command.Parameters.AddWithValue("@productId", alert.ProductId);
            command.Parameters.AddWithValue("@alertType", (int)alert.AlertType);
            command.Parameters.AddWithValue("@variancePercent", alert.VariancePercent);
            command.Parameters.AddWithValue("@costFifoSnapshot", alert.CostFifoSnapshot);
            command.Parameters.AddWithValue("@costNifoSnapshot", alert.CostNifoSnapshot);
            command.Parameters.AddWithValue("@currentStock", alert.CurrentStock);
            command.Parameters.AddWithValue("@status", (int)alert.Status);
            command.Parameters.AddWithValue("@resolvedBy", alert.ResolvedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@resolvedAt", alert.ResolvedAt ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@resolvedNote", alert.ResolvedNote ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", alert.CreatedAt);
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static PricingAlert MapFromReader(MySqlDataReader reader)
        {
            return new PricingAlert
            {
                Id = reader.GetInt32("id"),
                ProductId = reader.GetInt32("product_id"),
                AlertType = (AlertType)reader.GetInt32("alert_type"),
                VariancePercent = reader.GetDecimal("variance_percent"),
                CostFifoSnapshot = reader.GetDecimal("cost_fifo_snapshot"),
                CostNifoSnapshot = reader.GetDecimal("cost_nifo_snapshot"),
                CurrentStock = reader.GetInt32("current_stock"),
                Status = (AlertStatus)reader.GetInt32("status"),
                ResolvedBy = reader.IsDBNull(reader.GetOrdinal("resolved_by")) ? null : reader.GetInt32("resolved_by"),
                ResolvedAt = reader.IsDBNull(reader.GetOrdinal("resolved_at")) ? null : reader.GetDateTime("resolved_at"),
                ResolvedNote = reader.IsDBNull(reader.GetOrdinal("resolved_note")) ? null : reader.GetString("resolved_note"),
                CreatedAt = reader.GetDateTime("created_at")
            };
        }
    }
}
