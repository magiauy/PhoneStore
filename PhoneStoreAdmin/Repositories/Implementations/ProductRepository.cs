using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            using var command = new MySqlCommand(@"INSERT INTO products (sku, name, price, category_id, brand_id, status, created_at, cost, is_serial_tracked, warranty_months) VALUES (@sku, @name, @price, @categoryId, @brandId, @status, @createdAt, @cost, @isSerialTracked, @warrantyMonths); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@sku", entity.Sku);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@price", entity.Price);
            command.Parameters.AddWithValue("@categoryId", entity.CategoryId);
            command.Parameters.AddWithValue("@brandId", entity.BrandId);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@cost", entity.Cost);
            command.Parameters.AddWithValue("@isSerialTracked", entity.IsSerialTracked);
            command.Parameters.AddWithValue("@warrantyMonths", entity.WarrantyMonths);
            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Update(Product entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"UPDATE products SET sku = @sku, name = @name, price = @price, category_id = @categoryId, brand_id = @brandId, status = @status, created_at = @createdAt, cost = @cost, is_serial_tracked = @isSerialTracked, warranty_months = @warrantyMonths WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@sku", entity.Sku);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@price", entity.Price);
            command.Parameters.AddWithValue("@categoryId", entity.CategoryId);
            command.Parameters.AddWithValue("@brandId", entity.BrandId);
            command.Parameters.AddWithValue("@status", entity.Status.ToString());
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@cost", entity.Cost);
            command.Parameters.AddWithValue("@isSerialTracked", entity.IsSerialTracked);
            command.Parameters.AddWithValue("@warrantyMonths", entity.WarrantyMonths);
            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"DELETE FROM products WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
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
            command.Parameters.AddWithValue("@status", status.ToString());
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
                BrandId = reader.GetInt32("brand_id"),
                Status = statusEnum,
                CreatedAt = reader.GetDateTime("created_at"),
                Cost = reader.GetDecimal("cost"),
                IsSerialTracked = reader.GetBoolean("is_serial_tracked"),
                WarrantyMonths = reader.GetInt32("warranty_months"),
            };
        }

        #endregion
    }
}
