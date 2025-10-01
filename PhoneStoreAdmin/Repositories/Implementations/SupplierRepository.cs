using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Data;
using System;
using System.Collections.Generic;
using MySqlConnector;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly DataSource _dataSource;

        public SupplierRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        #region Basic CRUD

        // Tìm theo ID
        public Supplier GetById(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM suppliers WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                return MapFromReader(reader);
            }

            throw new InvalidOperationException($"Supplier with ID {id} not found.");
        }

        public IEnumerable<Supplier> GetAll()
        {
            var suppliers = new List<Supplier>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT * FROM suppliers", connection);
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                suppliers.Add(MapFromReader(reader));
            }
            return suppliers;
        }

        // Lấy selectbox
        public IEnumerable<Supplier> GetSelectBox()
        {
            var suppliers = new List<Supplier>();
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("SELECT id, name FROM suppliers ORDER BY Name", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                suppliers.Add(MapSelectBoxFromReader(reader));
            }

            return suppliers;
        }

        // Thêm
        public void Insert(Supplier entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"INSERT INTO suppliers (Name, Phone, Email, Address, Tax_Number, Is_Active) 
                  VALUES (@name, @phone, @email, @address, @taxNumber, @isActive)",
                connection);

            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@phone", entity.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@email", entity.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@address", entity.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@taxNumber", entity.TaxNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            command.ExecuteNonQuery();
        }

        // Sửa
        public void Update(Supplier entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                @"UPDATE suppliers 
                  SET Name = @name, Phone = @phone, Email = @email, Address = @address, 
                      Tax_Number = @taxNumber, Is_Active = @isActive 
                  WHERE Id = @id",
                connection);

            command.Parameters.AddWithValue("@id", entity.Id);
            command.Parameters.AddWithValue("@name", entity.Name);
            command.Parameters.AddWithValue("@phone", entity.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@email", entity.Email ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@address", entity.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@taxNumber", entity.TaxNumber ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@isActive", entity.IsActive);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Supplier with ID {entity.Id} not found for update.");
            }
        }

        // Xóa
        public void Delete(int id)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand("DELETE FROM suppliers WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@id", id);

            var rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"Supplier with ID {id} not found for deletion.");
            }
        }

        #endregion

        #region Filtered Search
        // Filter
        private (string whereClause, List<MySqlParameter> parameters) BuildConditions(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive)
        {
            var conditions = new List<string>();
            var parameters = new List<MySqlParameter>();

            if (!string.IsNullOrWhiteSpace(name))
            {
                conditions.Add("Name LIKE @name");
                parameters.Add(new MySqlParameter("@name", $"%{name}%"));
            }

            if (!string.IsNullOrWhiteSpace(phone))
            {
                conditions.Add("Phone LIKE @phone");
                parameters.Add(new MySqlParameter("@phone", $"%{phone}%"));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                conditions.Add("Email LIKE @email");
                parameters.Add(new MySqlParameter("@email", $"%{email}%"));
            }

            if (!string.IsNullOrWhiteSpace(address))
            {
                conditions.Add("Address LIKE @address");
                parameters.Add(new MySqlParameter("@address", $"%{address}%"));
            }

            if (!string.IsNullOrWhiteSpace(taxNumber))
            {
                conditions.Add("Tax_Number LIKE @taxNumber");
                parameters.Add(new MySqlParameter("@taxNumber", $"%{taxNumber}%"));
            }

            if (isActive.HasValue)
            {
                conditions.Add("Is_Active = @isActive");
                parameters.Add(new MySqlParameter("@isActive", isActive.Value));
            }

            var whereClause = conditions.Count > 0
                ? "WHERE " + string.Join(" AND ", conditions)
                : string.Empty;

            return (whereClause, parameters);
        }

        public int GetTotalRecords(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive)
        {
            var (whereClause, parameters) = BuildConditions(name, phone, email, address, taxNumber, isActive);
            var sql = $"SELECT COUNT(id) FROM suppliers {whereClause}";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);
            foreach (var param in parameters)
                command.Parameters.Add(param);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        public IEnumerable<Supplier> GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 20)
        {
            var suppliers = new List<Supplier>();
            var (whereClause, parameters) = BuildConditions(name, phone, email, address, taxNumber, isActive);

            var offset = (page - 1) * pageSize;
            var sql = $@"SELECT * FROM suppliers {whereClause} ORDER BY Id LIMIT @pageSize OFFSET @offset";

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(sql, connection);

            foreach (var param in parameters)
                command.Parameters.Add(param);

            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                suppliers.Add(MapFromReader(reader));
            }

            return suppliers;
        }

        public int GetTotalPages(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int pageSize)
        {
            var totalRecords = GetTotalRecords(name, phone, email, address, taxNumber, isActive);
            return (int)Math.Ceiling((double)totalRecords / pageSize);
        }

        #endregion

        #region Private Helper

        private static Supplier MapFromReader(MySqlDataReader reader)
        {
            return new Supplier
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
                TaxNumber = reader.IsDBNull(reader.GetOrdinal("tax_number")) ? null : reader.GetString("tax_number"),
                IsActive = reader.GetBoolean("is_active")
            };
        }

        private static Supplier MapSelectBoxFromReader(MySqlDataReader reader)
        {
            return new Supplier
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name")
            };
        }

        #endregion
    }
}
