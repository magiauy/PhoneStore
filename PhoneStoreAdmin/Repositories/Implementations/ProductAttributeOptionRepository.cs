using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductAttributeOptionRepository(DataSource dataSource) : IProductAttributeOptionRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductAttributeOption GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_options WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Product attribute option with ID {id} not found.");
        }

        public IEnumerable<ProductAttributeOption> GetAll()
        {
            var options = new List<ProductAttributeOption>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_options", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                options.Add(MapFromReader(reader));
            }

            return options;
        }

        public void Insert(ProductAttributeOption entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"INSERT INTO product_attribute_options
                    (attribute_id, display_value, normalized_value, sort_order, is_active)
                    VALUES (@attributeId, @displayValue, @normalizedValue, @sortOrder, @isActive)", connection);

            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@displayValue", entity.DisplayValue);
            command.Parameters.AddWithValue("@normalizedValue", entity.NormalizedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@sortOrder", entity.SortOrder);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(ProductAttributeOption entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"UPDATE product_attribute_options
                    SET attribute_id = @attributeId,
                        display_value = @displayValue,
                        normalized_value = @normalizedValue,
                        sort_order = @sortOrder,
                        is_active = @isActive
                    WHERE id = @id", connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@attributeId", entity.AttributeId);
            command.Parameters.AddWithValue("@displayValue", entity.DisplayValue);
            command.Parameters.AddWithValue("@normalizedValue", entity.NormalizedValue ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@sortOrder", entity.SortOrder);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute option with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_attribute_options WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute option with ID {id} not found for deletion.");
            }
        }

        public IEnumerable<ProductAttributeOption> GetByAttributeId(int attributeId)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_options WHERE attribute_id = @attributeId ORDER BY sort_order, display_value", connection);
            command.Parameters.AddWithValue("@attributeId", attributeId);

            using var reader = command.ExecuteReader();
            var options = new List<ProductAttributeOption>();
            while (reader.Read())
            {
                options.Add(MapFromReader(reader));
            }

            return options;
        }

        public IDictionary<int, IReadOnlyList<ProductAttributeOption>> GetByAttributeIds(IEnumerable<int> attributeIds)
        {
            var ids = attributeIds?.Distinct().ToList();
            if (ids == null || ids.Count == 0)
            {
                return new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            }

            var parameters = ids.Select((_, index) => $"@id{index}").ToList();
            var sql = $"SELECT * FROM product_attribute_options WHERE attribute_id IN ({string.Join(",", parameters)}) ORDER BY attribute_id, sort_order, display_value";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            for (var i = 0; i < ids.Count; i++)
            {
                command.Parameters.AddWithValue(parameters[i], ids[i]);
            }

            using var reader = command.ExecuteReader();
            var lookup = ids.ToDictionary(id => id, _ => (IReadOnlyList<ProductAttributeOption>)new List<ProductAttributeOption>());
            var temp = ids.ToDictionary(id => id, _ => new List<ProductAttributeOption>());

            while (reader.Read())
            {
                var option = MapFromReader(reader);
                if (!temp.TryGetValue(option.AttributeId, out var list))
                {
                    list = new List<ProductAttributeOption>();
                    temp[option.AttributeId] = list;
                }

                list.Add(option);
            }

            foreach (var pair in temp)
            {
                lookup[pair.Key] = pair.Value;
            }

            return lookup;
        }

        public ProductAttributeOption? FindByAttributeAndDisplay(int attributeId, string displayValue)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attribute_options WHERE attribute_id = @attributeId AND display_value = @displayValue LIMIT 1", connection);
            command.Parameters.AddWithValue("@attributeId", attributeId);
            command.Parameters.AddWithValue("@displayValue", displayValue);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        private static ProductAttributeOption MapFromReader(MySqlDataReader reader)
        {
            return new ProductAttributeOption
            {
                Id = reader.GetInt32("id"),
                AttributeId = reader.GetInt32("attribute_id"),
                DisplayValue = GetStringValue(reader, "display_value") ?? string.Empty,
                NormalizedValue = GetStringValue(reader, "normalized_value"),
                SortOrder = reader.IsDBNull(reader.GetOrdinal("sort_order")) ? 0 : reader.GetInt32("sort_order"),
                IsActive = reader.IsDBNull(reader.GetOrdinal("is_active")) ? true : reader.GetBoolean("is_active")
            };
        }

        private static string? GetStringValue(MySqlDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            return value switch
            {
                string s => s,
                _ => Convert.ToString(value, CultureInfo.InvariantCulture)
            };
        }

        private static int GetLastInsertedId(MySqlConnection connection)
        {
            using var command = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }
    }
}
