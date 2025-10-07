using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductCategoryRepository(DataSource dataSource) : IProductCategoryRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductCategory GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_categories WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Product Categories with ID {id} not found.");
        }

        public IEnumerable<ProductCategory> GetAll()
        {
            var productCategories = new List<ProductCategory>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_categories", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                productCategories.Add(MapFromReader(reader));
            }
            return productCategories;
        }

        public void Insert(ProductCategory entity)
        {
            throw new NotImplementedException();
        }

        public void Update(ProductCategory entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public ProductCategory GetByName(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductCategory> GetByParentId(int? parentId)
        {
            throw new NotImplementedException();
        }

        #region Private Helper

        private static ProductCategory MapFromReader(MySqlDataReader reader)
        {
            return new ProductCategory
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                ParentId = reader.IsDBNull(reader.GetOrdinal("parent_id")) ? null : reader.GetInt32("parent_id"),
                Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString("note"),
            };
        }

        #endregion
    }
}
