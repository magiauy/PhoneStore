using PhoneStoreRepository.Models;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using MySqlConnector;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class PromotionRepository : IPromotionRepository
    {
        private readonly DataSource _dataSource;

        public PromotionRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        public Promotion GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM promotions WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Promotion with ID {id} not found.");
        }

        public IEnumerable<Promotion> GetAll()
        {
            var promotions = new List<Promotion>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM promotions", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                promotions.Add(MapFromReader(reader));
            }
            return promotions;
        }

        public void Insert(Promotion entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO promotions (name, description, start_date, end_date, is_active) 
                  VALUES (@name, @description, @startDate, @endDate, @isActive)",
                connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@startDate", entity.StartDate);
            command.Parameters.AddWithValue("@endDate", entity.EndDate);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            command.ExecuteNonQuery();
        }

        public void Update(Promotion entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE promotions 
                  SET name = @name, description = @description, start_date = @startDate, 
                      end_date = @endDate, is_active = @isActive 
                  WHERE id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@startDate", entity.StartDate);
            command.Parameters.AddWithValue("@endDate", entity.EndDate);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Promotion with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM promotions WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Promotion with ID {id} not found for deletion.");
            }
        }

        public Promotion GetByName(string name)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM promotions WHERE name = @name", connection);
            command.Parameters.AddWithValue("@name", name);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Promotion with name '{name}' not found.");
        }

        #endregion

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("name LIKE @name");
                parameters.Add(new MySqlParameter("@name", $"%{name}%"));
            }

            if (isActive.HasValue)
            {
                conditions.Add("is_active = @isActive");
                parameters.Add(new MySqlParameter("@isActive", isActive.Value));
            }

            if (startDate.HasValue)
            {
                conditions.Add("start_date >= @startDate");
                parameters.Add(new MySqlParameter("@startDate", startDate.Value));
            }

            if (endDate.HasValue)
            {
                conditions.Add("end_date <= @endDate");
                parameters.Add(new MySqlParameter("@endDate", endDate.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public int GetTotalRecords(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate)
        {
            var (whereClause, parameters) = BuildConditions(name, isActive, startDate, endDate);
            var sql = $"SELECT COUNT(id) FROM promotions {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<Promotion> GetPromotionsFiltered(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 20)
        {
            var promotions = new List<Promotion>();
            var (whereClause, parameters) = BuildConditions(name, isActive, startDate, endDate);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM promotions {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                promotions.Add(MapFromReader(reader));
            }

            return promotions;
        }

        public int GetTotalPages(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(name, isActive, startDate, endDate);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static Promotion MapFromReader(MySqlDataReader reader)
        {
            return new Promotion
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                StartDate = reader.GetDateTime("start_date"),
                EndDate = reader.GetDateTime("end_date"),
                IsActive = reader.GetBoolean("is_active")
            };
        }

        #endregion
    }
}
