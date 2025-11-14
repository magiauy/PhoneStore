using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class ProductModelRepository(DataSource dataSource) : IProductModelRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductModel GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_models WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Product model with ID {id} not found.");
        }

        public IEnumerable<ProductModel> GetAll()
        {
            var models = new List<ProductModel>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_models ORDER BY name", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                models.Add(MapFromReader(reader));
            }

            return models;
        }

        public void Insert(ProductModel entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"INSERT INTO product_models
                    (name, slug, description, default_image_url, created_at, updated_at)
                    VALUES (@name, @slug, @description, @defaultImageUrl, @createdAt, @updatedAt)", connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@slug", entity.Slug);
            command.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@defaultImageUrl", entity.DefaultImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@updatedAt", entity.UpdatedAt);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(ProductModel entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"UPDATE product_models
                    SET name = @name,
                        slug = @slug,
                        description = @description,
                        default_image_url = @defaultImageUrl,
                        created_at = @createdAt,
                        updated_at = @updatedAt
                    WHERE id = @id", connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@slug", entity.Slug);
            command.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@defaultImageUrl", entity.DefaultImageUrl ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            command.Parameters.AddWithValue("@updatedAt", entity.UpdatedAt);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product model with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_models WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product model with ID {id} not found for deletion.");
            }
        }

        public ProductModel? FindBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                return null;
            }

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_models WHERE slug = @slug", connection);
            command.Parameters.AddWithValue("@slug", slug);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public IEnumerable<ProductModel> SearchByName(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Array.Empty<ProductModel>();
            }

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_models WHERE name LIKE @keyword ORDER BY name", connection);
            command.Parameters.AddWithValue("@keyword", $"%{keyword}%");

            using var reader = command.ExecuteReader();
            var models = new List<ProductModel>();
            while (reader.Read())
            {
                models.Add(MapFromReader(reader));
            }

            return models;
        }

        private static ProductModel MapFromReader(MySqlDataReader reader)
        {
            return new ProductModel
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Slug = reader.GetString("slug"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                DefaultImageUrl = reader.IsDBNull(reader.GetOrdinal("default_image_url")) ? null : reader.GetString("default_image_url"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
