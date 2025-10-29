using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductAttributeRepository(DataSource dataSource) : IProductAttributeRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductAttribute GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand("SELECT * FROM product_attributes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductAttribute with ID {id} not found.");
        }

        public IEnumerable<ProductAttribute> GetAll()
        {
            var attributes = new List<ProductAttribute>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand("SELECT * FROM product_attributes", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                attributes.Add(MapFromReader(reader));
            }
            return attributes;
        }

        public void Insert(ProductAttribute entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand(@"INSERT INTO product_attributes (name, data_type, note) VALUES (@name, @dataType, @note); SELECT LAST_INSERT_ID();", connection);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@dataType", entity.DataType.ToString());
            command.Parameters.AddWithValue("@note", (object?)entity.Note ?? DBNull.Value);
            var id = Convert.ToInt32(command.ExecuteScalar());
            entity.Id = id;
        }

        public void Update(ProductAttribute entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand(@"UPDATE product_attributes SET name = @name, data_type = @dataType, note = @note WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@dataType", entity.DataType.ToString());
            command.Parameters.AddWithValue("@note", (object?)entity.Note ?? DBNull.Value);
            command.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand(@"DELETE FROM product_attributes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);
            command.ExecuteNonQuery();
        }

        public ProductAttribute GetByName(string name)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlConnector.MySqlCommand("SELECT * FROM product_attributes WHERE name = @name", connection);
            command.Parameters.AddWithValue("@name", name);
            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }
            throw new InvalidOperationException($"ProductAttribute with name '{name}' not found.");
        }
        
        private static ProductAttribute MapFromReader(MySqlConnector.MySqlDataReader reader)
        {
            var dataTypeValue = reader.IsDBNull(reader.GetOrdinal("data_type")) ? "TEXT" : reader.GetString("data_type");
            if (!Enum.TryParse<PhoneStoreAdmin.Models.Enums.AttributeDataType>(dataTypeValue, true, out var dataTypeEnum))
                dataTypeEnum = PhoneStoreAdmin.Models.Enums.AttributeDataType.TEXT;
            return new ProductAttribute
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                DataType = dataTypeEnum,
                Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString("note"),
            };
        }
    }
}
