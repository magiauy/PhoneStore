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

            throw new InvalidOperationException($"Product attribute value with ID {id} not found.");
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
            using var command = new MySqlCommand(
                @"INSERT INTO product_attribute_values (product_id, attribute_id, option_id, value_text, value_number, value_date, value_bool)
                  VALUES (@productId, @attributeId, @optionId, @valueText, @valueNumber, @valueDate, @valueBool)",
                connection);

            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@optionId", entity.OptionId.HasValue ? entity.OptionId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueText", entity.ValueText ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueNumber", entity.ValueNumber.HasValue ? entity.ValueNumber.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueDate", entity.ValueDate.HasValue ? entity.ValueDate.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueBool", entity.ValueBool.HasValue ? entity.ValueBool.Value : (object)DBNull.Value);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(ProductAttributeValue entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE product_attribute_values
                  SET product_id = @productId,
                      attribute_id = @attributeId,
                      option_id = @optionId,
                      value_text = @valueText,
                      value_number = @valueNumber,
                      value_date = @valueDate,
                      value_bool = @valueBool
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@productId", entity.ProductId);
            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@optionId", entity.OptionId.HasValue ? entity.OptionId.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueText", entity.ValueText ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueNumber", entity.ValueNumber.HasValue ? entity.ValueNumber.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueDate", entity.ValueDate.HasValue ? entity.ValueDate.Value : (object)DBNull.Value);
            command.Parameters.AddWithValue("@valueBool", entity.ValueBool.HasValue ? entity.ValueBool.Value : (object)DBNull.Value);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute value with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_attribute_values WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute value with ID {id} not found for deletion.");
            }
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
                OptionId = reader.IsDBNull(reader.GetOrdinal("option_id")) ? null : reader.GetInt32("option_id"),
                ValueNumber = reader.IsDBNull(reader.GetOrdinal("value_number")) ? null : reader.GetDecimal("value_number"),
                ValueDate = reader.IsDBNull(reader.GetOrdinal("value_date")) ? null : reader.GetDateTime("value_date"),
                ValueBool = reader.IsDBNull(reader.GetOrdinal("value_bool")) ? null : reader.GetBoolean("value_bool")
            };
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
