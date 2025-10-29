using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductAttributeValueRepository(DataSource dataSource) : IProductAttributeValueRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductAttributeValue GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_values WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductAttributeValue with ID {id} not found.");
        }

        public IEnumerable<ProductAttributeValue> GetAll()
        {
            var values = new List<ProductAttributeValue>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_values", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                values.Add(MapFromReader(reader));
            }
            return values;
        }

        public void Insert(ProductAttributeValue entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"INSERT INTO product_attribute_values (product_id, attribute_id, value_text, value_number, value_date, value_bool) VALUES (@productId, @attributeId, @valueText, @valueNumber, @valueDate, @valueBool); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@valueText", (object?)entity.ValueText ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueNumber", (object?)entity.ValueNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueDate", (object?)entity.ValueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueBool", (object?)entity.ValueBool ?? DBNull.Value);
            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Update(ProductAttributeValue entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"UPDATE product_attribute_values SET product_id = @productId, attribute_id = @attributeId, value_text = @valueText, value_number = @valueNumber, value_date = @valueDate, value_bool = @valueBool WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@valueText", (object?)entity.ValueText ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueNumber", (object?)entity.ValueNumber ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueDate", (object?)entity.ValueDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@valueBool", (object?)entity.ValueBool ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"DELETE FROM product_attribute_values WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public IEnumerable<ProductAttributeValue> GetByProductId(int productId)
        {
            var values = new List<ProductAttributeValue>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_values WHERE product_id = @productId", connection);
            command.Parameters.AddWithValue("@productId", productId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                values.Add(MapFromReader(reader));
            }
            return values;
        }

        public IEnumerable<ProductAttributeValue> GetByAttributeId(int attributeId)
        {
            var values = new List<ProductAttributeValue>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_values WHERE attribute_id = @attributeId", connection);
            command.Parameters.AddWithValue("@attributeId", attributeId);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                values.Add(MapFromReader(reader));
            }
            return values;
        }

        private static ProductAttributeValue MapFromReader(MySqlDataReader reader)
        {
            return new ProductAttributeValue
            {
                Id = reader.GetInt32("id"),
                ProductId = reader.GetInt32("product_id"),
                AttributeId = reader.GetInt32("attribute_id"),
                ValueText = reader.IsDBNull(reader.GetOrdinal("value_text")) ? null : reader.GetString("value_text"),
                ValueNumber = reader.IsDBNull(reader.GetOrdinal("value_number")) ? null : reader.GetDecimal("value_number"),
                ValueDate = reader.IsDBNull(reader.GetOrdinal("value_date")) ? null : reader.GetDateTime("value_date"),
                ValueBool = reader.IsDBNull(reader.GetOrdinal("value_bool")) ? null : reader.GetBoolean("value_bool"),
            };
        }
    }
}
