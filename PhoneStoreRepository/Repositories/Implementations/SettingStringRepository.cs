using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class SettingStringRepository : ISettingStringRepository
    {
        private readonly DataSource _dataSource;

        public SettingStringRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        public SettingString GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM setting_string WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Setting with ID {id} not found.");
        }

        public IEnumerable<SettingString> GetAll()
        {
            var settings = new List<SettingString>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM setting_string ORDER BY code", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings.Add(MapFromReader(reader));
            }
            return settings;
        }

        public void Insert(SettingString entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO setting_string (code, value, type) VALUES (@code, @value, @type)",
                connection);

            command.Parameters.AddWithValue("@code", entity.Code);
            command.Parameters.AddWithValue("@value", entity.Value);
            command.Parameters.AddWithValue("@type", entity.Type);
            command.ExecuteNonQuery();

            // Get inserted ID
            using var idCommand = new MySqlCommand("SELECT LAST_INSERT_ID()", connection);
            entity.Id = Convert.ToInt32(idCommand.ExecuteScalar());
        }

        public void Update(SettingString entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE setting_string SET code = @code, value = @value, type = @type WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@code", entity.Code);
            command.Parameters.AddWithValue("@value", entity.Value);
            command.Parameters.AddWithValue("@type", entity.Type);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Setting with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM setting_string WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Setting with ID {id} not found for deletion.");
            }
        }

        #endregion

        #region Custom Methods

        public SettingString? GetByCode(string code)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM setting_string WHERE code = @code", connection);
            command.Parameters.AddWithValue("@code", code);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            return null;
        }

        public IEnumerable<SettingString> GetByType(string type)
        {
            var settings = new List<SettingString>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM setting_string WHERE type = @type ORDER BY code", connection);
            command.Parameters.AddWithValue("@type", type);
            
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings.Add(MapFromReader(reader));
            }
            return settings;
        }

        public bool CodeExists(string code, int? excludeId = null)
        {
            using var connection = _dataSource.GetConnection();
            var sql = excludeId.HasValue
                ? "SELECT COUNT(*) FROM setting_string WHERE code = @code AND id != @excludeId"
                : "SELECT COUNT(*) FROM setting_string WHERE code = @code";

            using var command = new MySqlCommand(sql, connection);
            command.Parameters.AddWithValue("@code", code);
            if (excludeId.HasValue)
            {
                command.Parameters.AddWithValue("@excludeId", excludeId.Value);
            }

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        #endregion

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(string? searchText)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                conditions.Add("(code LIKE @search OR value LIKE @search OR type LIKE @search)");
                parameters.Add(new MySqlParameter("@search", $"%{searchText}%"));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public int GetTotalRecords(string? searchText)
        {
            var (whereClause, parameters) = BuildConditions(searchText);
            var sql = $"SELECT COUNT(id) FROM setting_string {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<SettingString> GetSettingsFiltered(string? searchText, int page = 1, int pageSize = 20)
        {
            var settings = new List<SettingString>();
            var (whereClause, parameters) = BuildConditions(searchText);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM setting_string {whereClause} ORDER BY code LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                settings.Add(MapFromReader(reader));
            }

            return settings;
        }

        public int GetTotalPages(string? searchText, int pageSize)
        {
            var totalRecords = GetTotalRecords(searchText);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static SettingString MapFromReader(MySqlDataReader reader)
        {
            return new SettingString
            {
                Id = reader.GetInt32("id"),
                Code = reader.IsDBNull(reader.GetOrdinal("code")) ? string.Empty : reader.GetString("code"),
                Value = reader.IsDBNull(reader.GetOrdinal("value")) ? string.Empty : reader.GetString("value"),
                Type = reader.IsDBNull(reader.GetOrdinal("type")) ? string.Empty : reader.GetString("type")
            };
        }

        #endregion
    }
}
