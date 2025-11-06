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
            using var command = new MySqlCommand(
                @"INSERT INTO product_categories (name, parent_id, note)
                  VALUES (@name, @parentId, @note)",
                connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@parentId", entity.ParentId.HasValue ? entity.ParentId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(ProductCategory entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE product_categories
                  SET name = @name,
                      parent_id = @parentId,
                      note = @note
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@parentId", entity.ParentId.HasValue ? entity.ParentId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product category with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_categories WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product category with ID {id} not found for deletion.");
            }
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

            throw new InvalidOperationException($"Product category with name '{name}' not found.");
        }

        public IEnumerable<ProductCategory> GetByParentId(int? parentId)
        {
            var categories = new List<ProductCategory>();
            using var connection = _dataSource.GetConnection();
            string sql;
            if (parentId.HasValue)
            {
                sql = "SELECT * FROM product_categories WHERE parent_id = @parentId";
            }
            else
            {
                sql = "SELECT * FROM product_categories WHERE parent_id IS NULL";
            }

            using var command = new MySqlCommand(sql, connection);
            if (parentId.HasValue)
            {
                command.Parameters.AddWithValue("@parentId", parentId.Value);
            }

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(MapFromReader(reader));
            }

            return categories;
        }

        public IEnumerable<ProductCategory> GetCategoriesFiltered(string? name, int? parentId, int page = 1, int pageSize = 20)
        {
            var categories = new List<ProductCategory>();
            var (whereClause, parameters) = BuildConditions(name, parentId);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM product_categories {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(MapFromReader(reader));
            }

            return categories;
        }

        public int GetTotalRecords(string? name, int? parentId)
        {
            var (whereClause, parameters) = BuildConditions(name, parentId);
            var sql = $"SELECT COUNT(id) FROM product_categories {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetTotalPages(string? name, int? parentId, int pageSize)
        {
            var totalRecords = GetTotalRecords(name, parentId);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
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

        private static (string whereClause, List<MySqlParameter> parameters) BuildConditions(string? name, int? parentId)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("name LIKE @name");
                parameters.Add(new MySqlParameter("@name", $"%{name}%"));
            }

            if (parentId.HasValue)
            {
                conditions.Add("parent_id = @parentId");
                parameters.Add(new MySqlParameter("@parentId", parentId.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        #endregion
    }
}
