using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductRepository(DataSource dataSource) : IProductRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Product GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products where id=@id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            return null; // or throw an exception if product not found
        }

        public IEnumerable<Product> GetAll()
        {
            var products = new List<Product>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapFromReader(reader));
            }
            return products;
        }

        public void Insert(Product entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO products (sku, name, category_id, model_id, brand_id, price, cost, is_serial_tracked, warranty_months, status, created_at)
                  VALUES (@sku, @name, @categoryId, @modelId, @brandId, @price, @cost, @isSerialTracked, @warrantyMonths, @status, @createdAt)",
                connection);

            command.Parameters.AddWithValue("@sku", entity.Sku);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@categoryId", entity.CategoryId);
            command.Parameters.AddWithValue("@modelId", entity.ModelId);
            command.Parameters.AddWithValue("@brandId", entity.BrandId.HasValue ? entity.BrandId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@price", entity.Price);
            command.Parameters.AddWithValue("@cost", entity.Cost);
            command.Parameters.AddWithValue("@isSerialTracked", entity.IsSerialTracked);
            command.Parameters.AddWithValue("@warrantyMonths", entity.WarrantyMonths);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(Product entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE products
                  SET sku = @sku,
                      name = @name,
                      category_id = @categoryId,
                      model_id = @modelId,
                      brand_id = @brandId,
                      price = @price,
                      cost = @cost,
                      is_serial_tracked = @isSerialTracked,
                      warranty_months = @warrantyMonths,
                      status = @status,
                      created_at = @createdAt
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@sku", entity.Sku);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@categoryId", entity.CategoryId);
            command.Parameters.AddWithValue("@modelId", entity.ModelId);
            command.Parameters.AddWithValue("@brandId", entity.BrandId.HasValue ? entity.BrandId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@price", entity.Price);
            command.Parameters.AddWithValue("@cost", entity.Cost);
            command.Parameters.AddWithValue("@isSerialTracked", entity.IsSerialTracked);
            command.Parameters.AddWithValue("@warrantyMonths", entity.WarrantyMonths);
            command.Parameters.AddWithValue("@status", entity.Status.ToString().ToLower());
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM products WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product with ID {id} not found for deletion.");
            }
        }

        public Product GetBySku(string sku)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products WHERE sku = @sku", connection);
            command.Parameters.AddWithValue("@sku", sku);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public IEnumerable<Product> GetByCategoryId(int categoryId)
        {
            var products = new List<Product>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products WHERE category_id = @categoryId", connection);
            command.Parameters.AddWithValue("@categoryId", categoryId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapFromReader(reader));
            }

            return products;
        }

        public IEnumerable<Product> GetByBrandId(int brandId)
        {
            var products = new List<Product>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products WHERE brand_id = @brandId", connection);
            command.Parameters.AddWithValue("@brandId", brandId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapFromReader(reader));
            }

            return products;
        }

        public IEnumerable<Product> GetByStatus(PhoneStoreAdmin.Models.Enums.ProductStatus status)
        {
            var products = new List<Product>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products WHERE status = @status", connection);
            command.Parameters.AddWithValue("@status", status.ToString().ToLower());

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapFromReader(reader));
            }

            return products;
        }

        public IEnumerable<Product> GetByModelId(int modelId)
        {
            var products = new List<Product>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM products WHERE model_id = @modelId", connection);
            command.Parameters.AddWithValue("@modelId", modelId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapFromReader(reader));
            }

            return products;
        }

        #region Private Helper

        private static Product MapFromReader(MySqlDataReader reader)
        {
            var statusValue = reader.IsDBNull(reader.GetOrdinal("status"))
                ? "ACTIVE"
                : reader.GetString("status");

            if (!Enum.TryParse<ProductStatus>(statusValue, true, out var statusEnum))
                statusEnum = ProductStatus.ACTIVE; 

            return new Product
            {
                Id = reader.GetInt32("id"),
                Sku = reader.GetString("sku"),
                Name = reader.GetString("name"),
                Price = reader.GetDecimal("price"),
                CategoryId = reader.GetInt32("category_id"),
                ModelId = reader.IsDBNull(reader.GetOrdinal("model_id")) ? 0 : reader.GetInt32("model_id"),
                BrandId = reader.IsDBNull(reader.GetOrdinal("brand_id")) ? null : reader.GetInt32("brand_id"),
                Status = statusEnum,
                CreatedAt = reader.GetDateTime("created_at"),
                Cost = reader.GetDecimal("cost"),
                IsSerialTracked = reader.GetBoolean("is_serial_tracked"),
                WarrantyMonths = reader.GetInt32("warranty_months"),
            };
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion
    }
}
