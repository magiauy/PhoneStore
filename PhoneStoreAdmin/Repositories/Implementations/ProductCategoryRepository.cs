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
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"INSERT INTO product_categories (name, parent_id, note) VALUES (@name, @parentId, @note); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@parentId", (object?)entity.ParentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@note", (object?)entity.Note ?? DBNull.Value);
            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Update(ProductCategory entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"UPDATE product_categories SET name = @name, parent_id = @parentId, note = @note WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@parentId", (object?)entity.ParentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@note", (object?)entity.Note ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"DELETE FROM product_categories WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public ProductCategory GetByName(string name)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_categories WHERE name = @name", connection);
            command.Parameters.AddWithValue("@name", name);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"Product Category with name '{name}' not found.");
        }

        public IEnumerable<ProductCategory> GetByParentId(int? parentId)
        {
            var categories = new List<ProductCategory>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_categories WHERE parent_id = @parentId", connection);
            command.Parameters.AddWithValue("@parentId", (object?)parentId ?? DBNull.Value);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(MapFromReader(reader));
            }
            return categories;
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
