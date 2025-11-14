using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class BrandRepository : IBrandRepository
    {
        private readonly DataSource _dataSource;

        public BrandRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        public Brand GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM brands WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Brand with ID {id} not found.");
        }

        public IEnumerable<Brand> GetAll()
        {
            var brands = new List<Brand>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM brands", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                brands.Add(MapFromReader(reader));
            }
            return brands;
        }

        public void Insert(Brand entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO brands (name) VALUES (@name)",
                connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.ExecuteNonQuery();
        }

        public void Update(Brand entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE brands SET name = @name WHERE Id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Brand with ID {entity.Id} not found for update.");
            }
        }

        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM brands WHERE id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Brand with ID {id} not found for deletion.");
            }
        }

        public Brand GetByName(string name)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM brands WHERE name = @name", connection);
            command.Parameters.AddWithValue("@name", name);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Brand with name '{name}' not found.");
        }

        #endregion

        #region Filtered Search

        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(string? name)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("name LIKE @name");
                parameters.Add(new MySqlParameter("@name", $"%{name}%"));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public int GetTotalRecords(string? name)
        {
            var (whereClause, parameters) = BuildConditions(name);
            var sql = $"SELECT COUNT(id) FROM brands {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<Brand> GetBrandsFiltered(string? name, int page = 1, int pageSize = 20)
        {
            var brands = new List<Brand>();
            var (whereClause, parameters) = BuildConditions(name);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM brands {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                brands.Add(MapFromReader(reader));
            }

            return brands;
        }

        public int GetTotalPages(string? name, int pageSize)
        {
            var totalRecords = GetTotalRecords(name);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static Brand MapFromReader(MySqlDataReader reader)
        {
            return new Brand
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
            };
        }

        #endregion
    }
}
