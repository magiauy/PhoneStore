using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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

        #region Synchronous Methods

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
            using var command = new MySqlCommand("SELECT * FROM suppliers ORDER BY Name", connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                suppliers.Add(MapFromReader(reader));
            }

            return suppliers;
        }

        public IEnumerable<Supplier> GetSuppliers(int page, int pageSize)
        {
            var suppliers = new List<Supplier>();
            var offset = (page - 1) * pageSize;

            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(
                "SELECT * FROM suppliers ORDER BY Name LIMIT @pageSize OFFSET @offset", 
                connection);
            
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);
            
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                suppliers.Add(MapFromReader(reader));
            }

            return suppliers;
        }

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

        #region Asynchronous Methods

        public async Task<IEnumerable<Supplier>> GetAllAsync()
        {
            return await Task.Run(() => GetAll());
        }

        public async Task<Supplier?> GetByIdAsync(int id)
        {
            return await Task.Run(() =>
            {
                try
                {
                    return GetById(id);
                }
                catch (InvalidOperationException)
                {
                    return null;
                }
            });
        }

        public async Task<Supplier?> AddAsync(Supplier entity)
        {
            return await Task.Run(() =>
            {
                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand(
                    @"INSERT INTO suppliers (Name, Phone, Email, Address, Tax_Number, Is_Active) 
                      VALUES (@name, @phone, @email, @address, @taxNumber, @isActive); 
                      SELECT LAST_INSERT_ID();", 
                    connection);

                command.Parameters.AddWithValue("@name", entity.Name);
                command.Parameters.AddWithValue("@phone", entity.Phone ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@email", entity.Email ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@address", entity.Address ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@taxNumber", entity.TaxNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@isActive", entity.IsActive);

                var newId = Convert.ToInt32(command.ExecuteScalar());
                entity.Id = newId;
                return entity;
            });
        }

        public async Task UpdateAsync(Supplier entity)
        {
            await Task.Run(() => Update(entity));
        }

        public async Task DeleteAsync(int id)
        {
            await Task.Run(() => Delete(id));
        }

        #endregion

        #region Filtered Search Methods

        public async Task<SupplierResult> GetSuppliersFiltered(
    string? name,
    string? phone,
    string? email,
    string? address,
    string? taxNumber,
    bool? isActive,
    int page = 1,
    int pageSize = 20)
        {
            return await Task.Run(() =>
            {
                var suppliers = new List<Supplier>();
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

                var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

                // Lấy totalRecords
                var countSql = $"SELECT COUNT(*) FROM suppliers {whereClause}";
                int totalRecords;
                using (var connection = _dataSource.GetConnection())
                {
                    using var countCommand = new MySqlCommand(countSql, connection);
                    foreach (var param in parameters)
                        countCommand.Parameters.Add(param);

                    totalRecords = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                // Lấy dữ liệu
                var offset = (page - 1) * pageSize;
                var sql = $@"SELECT * FROM suppliers {whereClause} ORDER BY id LIMIT @pageSize OFFSET @offset";

                using (var connection = _dataSource.GetConnection())
                {
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
                }

                var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
                var info = new InfoTable(totalRecords, totalPages);

                return new SupplierResult(suppliers, info);
            });
        }



        public async Task<int> GetTotalPages(
            string? name, 
            string? phone, 
            string? email, 
            string? address, 
            string? taxNumber, 
            bool? isActive, 
            int pageSize)
        {
            return await Task.Run(() =>
            {
                var conditions = new List<string>();
                var parameters = new List<MySqlParameter>();

                // Build WHERE conditions (same as GetSuppliersFiltered)
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

                var whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
                var sql = $"SELECT COUNT(*) FROM suppliers {whereClause}";

                using var connection = _dataSource.GetConnection();
                using var command = new MySqlCommand(sql, connection);
                
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                var totalRecords = Convert.ToInt32(command.ExecuteScalar());
                
                return (int)Math.Ceiling((double)totalRecords / pageSize);
            });
        }

        #endregion

        #region Private Helper Methods

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

        #endregion
    }

    public class InfoTable
    {
        public readonly int totalRecords;
        public readonly int totalPages;

        public InfoTable(int totalRecords, int totalPages)
        {
            this.totalRecords = totalRecords;
            this.totalPages = totalPages;
        }
    }

    public class SupplierResult
    {
        public IEnumerable<Supplier> Suppliers { get; set; }
        public InfoTable Info { get; set; }

        public SupplierResult(IEnumerable<Supplier> suppliers, InfoTable info)
        {
            Suppliers = suppliers;
            Info = info;
        }
    }
}
