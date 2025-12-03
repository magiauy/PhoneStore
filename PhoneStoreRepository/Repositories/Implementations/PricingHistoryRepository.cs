using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation cho quản lý lịch sử biến động giá
    /// </summary>
    public class PricingHistoryRepository : IPricingHistoryRepository
    {
        private readonly DataSource _dataSource;

        public PricingHistoryRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void Insert(PricingHistory history)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO pricing_history 
                  (product_id, old_price, new_price, cost_fifo, cost_nifo, 
                   change_reason, changed_by, created_at) 
                  VALUES 
                  (@productId, @oldPrice, @newPrice, @costFifo, @costNifo, 
                   @changeReason, @changedBy, @createdAt)",
                connection);
            command.Parameters.AddWithValue("@productId", history.ProductId);
            command.Parameters.AddWithValue("@oldPrice", history.OldPrice);
            command.Parameters.AddWithValue("@newPrice", history.NewPrice);
            command.Parameters.AddWithValue("@costFifo", history.CostFifo);
            command.Parameters.AddWithValue("@costNifo", history.CostNifo);
            command.Parameters.AddWithValue("@changeReason", history.ChangeReason);
            command.Parameters.AddWithValue("@changedBy", history.ChangedBy ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", history.CreatedAt);
            command.ExecuteNonQuery();
            history.Id = GetLastInsertedId(connection);
        }


        public IReadOnlyList<PricingHistory> GetByProduct(int productId, int limit = 50)
        {
            var result = new List<PricingHistory>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM pricing_history WHERE product_id = @productId ORDER BY created_at DESC LIMIT @limit",
                connection);
            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@limit", limit);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public IReadOnlyList<PricingHistory> GetRecent(int limit = 100)
        {
            var result = new List<PricingHistory>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM pricing_history ORDER BY created_at DESC LIMIT @limit",
                connection);
            command.Parameters.AddWithValue("@limit", limit);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static PricingHistory MapFromReader(MySqlDataReader reader)
        {
            return new PricingHistory
            {
                Id = reader.GetInt32("id"),
                ProductId = reader.GetInt32("product_id"),
                OldPrice = reader.GetDecimal("old_price"),
                NewPrice = reader.GetDecimal("new_price"),
                CostFifo = reader.GetDecimal("cost_fifo"),
                CostNifo = reader.GetDecimal("cost_nifo"),
                ChangeReason = reader.GetString("change_reason"),
                ChangedBy = reader.IsDBNull(reader.GetOrdinal("changed_by")) ? null : reader.GetInt32("changed_by"),
                CreatedAt = reader.GetDateTime("created_at")
            };
        }
    }
}
