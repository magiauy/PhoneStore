using System;
using System.Collections.Generic;
using System.Data;
using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class BatchProductRepository : IBatchProductRepository
    {
        private readonly DataSource _dataSource;

        public BatchProductRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public IEnumerable<BatchProduct> GetAll()
        {
            var result = new List<BatchProduct>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batch_products", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public BatchProduct GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batch_products WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"BatchProduct with ID {id} not found.");
        }

        public ICollection<BatchProduct> GetByBatchId(int batchId)
        {
            var result = new List<BatchProduct>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM batch_products WHERE batch_id = @batchId", connection);
            command.Parameters.AddWithValue("@batchId", batchId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                result.Add(MapFromReader(reader));
            }
            return result;
        }

        public void Insert(BatchProduct entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO batch_products (batch_id, product_id, quantity, cost_price, selling_price, profit_margin) 
                  VALUES (@batchId, @productId, @quantity, @costPrice, @sellingPrice, @profitMargin)",
                connection);
            command.Parameters.AddWithValue("@batchId", entity.BatchId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@costPrice", entity.CostPrice);
            command.Parameters.AddWithValue("@sellingPrice", entity.SellingPrice);
            command.Parameters.AddWithValue("@profitMargin", entity.ProfitMargin);
            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Insert(BatchProduct entity, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand(
                @"INSERT INTO batch_products (batch_id, product_id, quantity, cost_price, selling_price, profit_margin) 
                  VALUES (@batchId, @productId, @quantity, @costPrice, @sellingPrice, @profitMargin)",
                connection, transaction);
            command.Parameters.AddWithValue("@batchId", entity.BatchId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@costPrice", entity.CostPrice);
            command.Parameters.AddWithValue("@sellingPrice", entity.SellingPrice);
            command.Parameters.AddWithValue("@profitMargin", entity.ProfitMargin);
            command.ExecuteNonQuery();

            // Get last inserted ID with transaction
            using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection, transaction);
            entity.Id = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        public void Update(BatchProduct entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE batch_products 
                  SET batch_id = @batchId, product_id = @productId, quantity = @quantity, 
                      cost_price = @costPrice, selling_price = @sellingPrice, profit_margin = @profitMargin
                  WHERE id = @id",
                connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@batchId", entity.BatchId);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@quantity", entity.Quantity);
            command.Parameters.AddWithValue("@costPrice", entity.CostPrice);
            command.Parameters.AddWithValue("@sellingPrice", entity.SellingPrice);
            command.Parameters.AddWithValue("@profitMargin", entity.ProfitMargin);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"BatchProduct with ID {entity.Id} not found for update.");
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM batch_products WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            var rows = command.ExecuteNonQuery();
            if (rows == 0)
                throw new InvalidOperationException($"BatchProduct with ID {id} not found for deletion.");
        }

        public void DeleteByBatchId(int batchId)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM batch_products WHERE batch_id = @batchId", connection);
            command.Parameters.AddWithValue("@batchId", batchId);
            command.ExecuteNonQuery();
        }

        public void DeleteByBatchId(int batchId, MySqlConnection connection, MySqlTransaction transaction)
        {
            using var command = new MySqlCommand("DELETE FROM batch_products WHERE batch_id = @batchId", connection, transaction);
            command.Parameters.AddWithValue("@batchId", batchId);
            command.ExecuteNonQuery();
        }

        public void DecreaseQuantity(int batchId, int productId, int amount = 1)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
            }

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                UPDATE batch_products
                SET quantity = quantity - @amount
                WHERE batch_id = @batchId AND product_id = @productId AND quantity >= @amount
                ORDER BY id ASC
                LIMIT 1", connection);

            command.Parameters.AddWithValue("@batchId", batchId);
            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@amount", amount);

            var affected = command.ExecuteNonQuery();
            if (affected == 0)
            {
                throw new InvalidOperationException($"Batch product not found or does not have enough stock for batch {batchId} and product {productId}.");
            }
        }

        public void DecreaseQuantity(int batchId, int productId, int amount, MySqlConnection connection, MySqlTransaction transaction)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
            }

            using var command = new MySqlCommand(@"
                UPDATE batch_products
                SET quantity = quantity - @amount
                WHERE batch_id = @batchId AND product_id = @productId AND quantity >= @amount
                ORDER BY id ASC
                LIMIT 1", connection, transaction);

            command.Parameters.AddWithValue("@batchId", batchId);
            command.Parameters.AddWithValue("@productId", productId);
            command.Parameters.AddWithValue("@amount", amount);

            var affected = command.ExecuteNonQuery();
            if (affected == 0)
            {
                throw new InvalidOperationException($"Batch product not found or does not have enough stock for batch {batchId} and product {productId}.");
            }
        }

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(
            int? batchId,
            int? productId,
            int? minQuantity,
            int? maxQuantity,
            decimal? minCostPrice,
            decimal? maxCostPrice,
            decimal? minSellingPrice,
            decimal? maxSellingPrice)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (batchId.HasValue)
            {
                conditions.Add("batch_id = @batchId");
                parameters.Add(new MySqlParameter("@batchId", batchId.Value));
            }

            if (productId.HasValue)
            {
                conditions.Add("product_id = @productId");
                parameters.Add(new MySqlParameter("@productId", productId.Value));
            }

            if (minQuantity.HasValue)
            {
                conditions.Add("quantity >= @minQuantity");
                parameters.Add(new MySqlParameter("@minQuantity", minQuantity.Value));
            }

            if (maxQuantity.HasValue)
            {
                conditions.Add("quantity <= @maxQuantity");
                parameters.Add(new MySqlParameter("@maxQuantity", maxQuantity.Value));
            }

            if (minCostPrice.HasValue)
            {
                conditions.Add("cost_price >= @minCostPrice");
                parameters.Add(new MySqlParameter("@minCostPrice", minCostPrice.Value));
            }

            if (maxCostPrice.HasValue)
            {
                conditions.Add("cost_price <= @maxCostPrice");
                parameters.Add(new MySqlParameter("@maxCostPrice", maxCostPrice.Value));
            }

            if (minSellingPrice.HasValue)
            {
                conditions.Add("selling_price >= @minSellingPrice");
                parameters.Add(new MySqlParameter("@minSellingPrice", minSellingPrice.Value));
            }

            if (maxSellingPrice.HasValue)
            {
                conditions.Add("selling_price <= @maxSellingPrice");
                parameters.Add(new MySqlParameter("@maxSellingPrice", maxSellingPrice.Value));
            }

            var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : string.Empty;
            return (whereClause, parameters);
        }

        public int GetTotalRecords(int? batchId, int? productId, int? minQuantity, int? maxQuantity, decimal? minCostPrice, decimal? maxCostPrice, decimal? minSellingPrice, decimal? maxSellingPrice)
        {
            var (whereClause, parameters) = BuildConditions(batchId, productId, minQuantity, maxQuantity, minCostPrice, maxCostPrice, minSellingPrice, maxSellingPrice);
            var sql = $"SELECT COUNT(*) FROM batch_products {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<BatchProduct> GetBatchProductsFiltered(
            int? batchId,
            int? productId,
            int? minQuantity,
            int? maxQuantity,
            decimal? minCostPrice,
            decimal? maxCostPrice,
            decimal? minSellingPrice,
            decimal? maxSellingPrice,
            int page = 1,
            int pageSize = 20)
        {
            var list = new List<BatchProduct>();
            var (whereClause, parameters) = BuildConditions(batchId, productId, minQuantity, maxQuantity, minCostPrice, maxCostPrice, minSellingPrice, maxSellingPrice);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM batch_products {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

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
            int? batchId,
            int? productId,
            int? minQuantity,
            int? maxQuantity,
            decimal? minCostPrice,
            decimal? maxCostPrice,
            decimal? minSellingPrice,
            decimal? maxSellingPrice,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(batchId, productId, minQuantity, maxQuantity, minCostPrice, maxCostPrice, minSellingPrice, maxSellingPrice);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        private int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static BatchProduct MapFromReader(MySqlDataReader reader)
        {
            return new BatchProduct
            {
                Id = reader.GetInt32("id"),
                BatchId = reader.GetInt32("batch_id"),
                ProductId = reader.GetInt32("product_id"),
                Quantity = reader.GetInt32("quantity"),
                CostPrice = reader.GetDecimal("cost_price"),
                SellingPrice = reader.GetDecimal("selling_price"),
                ProfitMargin = reader.IsDBNull(reader.GetOrdinal("profit_margin")) ? 0m : (decimal)reader.GetFloat("profit_margin")
            };
        }

        public IDictionary<int, int> GetQuantitiesByProductIds(IEnumerable<int> productIds)
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

            var query = $"SELECT product_id, SUM(quantity) as total_qty FROM batch_products WHERE product_id IN ({string.Join(",", parameterNames)}) GROUP BY product_id";

            using var command = new MySqlCommand(query, connection);
            for (int i = 0; i < ids.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], ids[i]);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var productId = reader.GetInt32("product_id");
                var totalQty = reader.IsDBNull(reader.GetOrdinal("total_qty")) ? 0 : reader.GetInt32("total_qty");
                counts[productId] = totalQty;
            }

            return counts;
        }
    }
}
