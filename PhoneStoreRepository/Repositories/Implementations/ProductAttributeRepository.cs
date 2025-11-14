using MySqlConnector;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using PhoneStoreRepository.Utils;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class ProductAttributeRepository(DataSource dataSource) : IProductAttributeRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductAttribute GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attributes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Product attribute with ID {id} not found.");
        }

        public IEnumerable<ProductAttribute> GetAll()
        {
            var attributes = new List<ProductAttribute>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attributes", connection);
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
            using var command = new MySqlCommand(
                @"INSERT INTO product_attributes (name, data_type, note)
                  VALUES (@name, @dataType, @note)",
                connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@dataType", MapDataTypeToString(entity.DataType));
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
            entity.Id = GetLastInsertedId(connection);
        }

        public void Update(ProductAttribute entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE product_attributes
                  SET name = @name,
                      data_type = @dataType,
                      note = @note
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@dataType", MapDataTypeToString(entity.DataType));
            command.Parameters.AddWithValue("@note", entity.Note ?? (object)DBNull.Value);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM product_attributes WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rows = command.ExecuteNonQuery();
            if (rows == 0)
            {
                throw new InvalidOperationException($"Product attribute with ID {id} not found for deletion.");
            }
        }

        public ProductAttribute GetByName(string name)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM product_attributes WHERE name = @name", connection);
            command.Parameters.AddWithValue("@name", name);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Product attribute with name '{name}' not found.");
        }

        public IDictionary<string, ProductAttribute> GetByNames(IEnumerable<string> names)
        {
            var nameList = names?
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (nameList == null || nameList.Count == 0)
            {
                return new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);
            }

            var parameterNames = nameList.Select((_, index) => $"@name{index}").ToList();
            var sql = $"SELECT * FROM product_attributes WHERE name IN ({string.Join(",", parameterNames)})";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            for (var i = 0; i < nameList.Count; i++)
            {
                command.Parameters.AddWithValue(parameterNames[i], nameList[i]);
            }

            var result = new Dictionary<string, ProductAttribute>(StringComparer.OrdinalIgnoreCase);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var attribute = MapFromReader(reader);
                result[attribute.Name] = attribute;
            }

            return result;
        }

        public IEnumerable<ProductAttribute> GetAttributesFiltered(string? name, AttributeDataType? dataType, int page = 1, int pageSize = 20)
        {
            var attributes = new List<ProductAttribute>();
            var (whereClause, parameters) = BuildConditions(name, dataType);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM product_attributes {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

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
                attributes.Add(MapFromReader(reader));
            }

            return attributes;
        }

        public int GetTotalRecords(string? name, AttributeDataType? dataType)
        {
            var (whereClause, parameters) = BuildConditions(name, dataType);
            var sql = $"SELECT COUNT(id) FROM product_attributes {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public int GetTotalPages(string? name, AttributeDataType? dataType, int pageSize)
        {
            var totalRecords = GetTotalRecords(name, dataType);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #region Private Helper

        private static ProductAttribute MapFromReader(MySqlDataReader reader)
        {
            return new ProductAttribute
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                DataType = MapStringToDataType(reader.GetString("data_type")),
                Note = reader.IsDBNull(reader.GetOrdinal("note")) ? null : reader.GetString("note"),
            };
        }

        private static AttributeDataType MapStringToDataType(string dataType)
        {
            var normalizedType = dataType?.Trim().ToLower() ?? "text";
            var result = normalizedType switch
            {
                "text" => AttributeDataType.TEXT,
                "number" => AttributeDataType.NUMBER,
                "date" => AttributeDataType.DATE,
                "bool" => AttributeDataType.BOOLEAN,
                _ => AttributeDataType.TEXT
            };

            if (result == AttributeDataType.TEXT && normalizedType != "text")
            {
                Logger.Error($"Unknown data type from database: '{dataType}' (normalized: '{normalizedType}'). Defaulting to TEXT.");
            }

            return result;
        }

        private static string MapDataTypeToString(AttributeDataType dataType)
        {
            return dataType switch
            {
                AttributeDataType.TEXT => "text",
                AttributeDataType.NUMBER => "number",
                AttributeDataType.DATE => "date",
                AttributeDataType.BOOLEAN => "bool",
                _ => "text",
            };
        }

        private static (string whereClause, List<MySqlParameter> parameters) BuildConditions(string? name, AttributeDataType? dataType)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("name LIKE @name");
                parameters.Add(new MySqlParameter("@name", $"%{name}%"));
            }

            if (dataType.HasValue)
            {
                conditions.Add("data_type = @dataType");
                parameters.Add(new MySqlParameter("@dataType", MapDataTypeToString(dataType.Value)));
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
