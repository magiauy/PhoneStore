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
            throw new NotImplementedException();
        }

        public void Update(Product entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Product GetBySku(string sku)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByCategoryId(int categoryId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByBrandId(int brandId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByStatus(PhoneStoreAdmin.Models.Enums.ProductStatus status)
        {
            throw new NotImplementedException();
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
