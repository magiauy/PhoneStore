using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using MySqlConnector;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly DataSource _dataSource;

        public SupplierRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public Supplier GetById(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand("SELECT * FROM suppliers WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapSupplier(reader);
            throw new KeyNotFoundException($"Supplier with ID {id} not found");
        }

        public IEnumerable<Supplier> GetAll()
        {
            var list = new List<Supplier>();
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand("SELECT * FROM suppliers", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) list.Add(MapSupplier(reader));
            return list;
        }

        public void Insert(Supplier entity)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand(
                @"INSERT INTO suppliers (name, phone, email, address, tax_number, is_active) 
                  VALUES (@name,@phone,@email,@address,@tax_number,@is_active)", conn);

            cmd.Parameters.AddWithValue("@name", entity.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", entity.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", entity.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", entity.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tax_number", entity.TaxNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@is_active", entity.IsActive);

            cmd.ExecuteNonQuery();
            entity.Id = (int)cmd.LastInsertedId;
        }

        public void Update(Supplier entity)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand(
                @"UPDATE suppliers SET name=@name, phone=@phone, email=@email, 
                  address=@address, tax_number=@tax_number, is_active=@is_active 
                  WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("@name", entity.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@phone", entity.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@email", entity.Email ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@address", entity.Address ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@tax_number", entity.TaxNumber ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@is_active", entity.IsActive);

            if (cmd.ExecuteNonQuery() == 0) throw new KeyNotFoundException($"Supplier with ID {entity.Id} not found");
        }

        public void Delete(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand("DELETE FROM suppliers WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            if (cmd.ExecuteNonQuery() == 0) throw new KeyNotFoundException($"Supplier with ID {id} not found");
        }

        public void DeActivate(int id) => UpdateActiveStatus(id, false);
        public void Activate(int id) => UpdateActiveStatus(id, true);

        private void UpdateActiveStatus(int id, bool status)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand("UPDATE suppliers SET is_active=@status WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            cmd.Parameters.AddWithValue("@status", status);
            if (cmd.ExecuteNonQuery() == 0) throw new KeyNotFoundException($"Supplier with ID {id} not found");
        }

        public Supplier GetByName(string name)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = new MySqlCommand("SELECT * FROM suppliers WHERE name=@name", conn);
            cmd.Parameters.AddWithValue("@name", name);

            using var reader = cmd.ExecuteReader();
            if (reader.Read()) return MapSupplier(reader);
            throw new KeyNotFoundException($"Supplier with name '{name}' not found");
        }

        public IEnumerable<Supplier> GetSuppliersFiltered(
            string? name = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? taxNumber = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 10)

        {
            var suppliers = new List<Supplier>();
            using var conn = _dataSource.GetConnection();
            var sql = "SELECT * FROM suppliers WHERE 1=1";
            var cmd = new MySqlCommand { Connection = conn };

            if (!string.IsNullOrWhiteSpace(name)) { sql += " AND name LIKE @name"; cmd.Parameters.AddWithValue("@name", $"%{name}%"); }
            if (!string.IsNullOrWhiteSpace(phone)) { sql += " AND phone LIKE @phone"; cmd.Parameters.AddWithValue("@phone", $"%{phone}%"); }
            if (!string.IsNullOrWhiteSpace(email)) { sql += " AND email LIKE @email"; cmd.Parameters.AddWithValue("@email", $"%{email}%"); }
            if (!string.IsNullOrWhiteSpace(address)) { sql += " AND address LIKE @address"; cmd.Parameters.AddWithValue("@address", $"%{address}%"); }
            if (!string.IsNullOrWhiteSpace(taxNumber)) { sql += " AND tax_number LIKE @tax_number"; cmd.Parameters.AddWithValue("@tax_number", $"%{taxNumber}%"); }
            if (isActive.HasValue) { sql += " AND is_active=@is_active"; cmd.Parameters.AddWithValue("@is_active", isActive.Value); }

            sql += " ORDER BY id LIMIT @offset,@pageSize";
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read()) suppliers.Add(MapSupplier(reader));

            return suppliers;
        }

        public int GetTotalPages(string? name = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? taxNumber = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 10)
        {
            using var conn = _dataSource.GetConnection();
            var sql = "SELECT COUNT(*) FROM suppliers WHERE 1=1";
            var cmd = new MySqlCommand { Connection = conn };

            if (!string.IsNullOrWhiteSpace(name)) { sql += " AND name LIKE @name"; cmd.Parameters.AddWithValue("@name", $"%{name}%"); }
            if (!string.IsNullOrWhiteSpace(phone)) { sql += " AND phone LIKE @phone"; cmd.Parameters.AddWithValue("@phone", $"%{phone}%"); }
            if (!string.IsNullOrWhiteSpace(email)) { sql += " AND email LIKE @email"; cmd.Parameters.AddWithValue("@email", $"%{email}%"); }
            if (!string.IsNullOrWhiteSpace(address)) { sql += " AND address LIKE @address"; cmd.Parameters.AddWithValue("@address", $"%{address}%"); }
            if (!string.IsNullOrWhiteSpace(taxNumber)) { sql += " AND tax_number LIKE @tax_number"; cmd.Parameters.AddWithValue("@tax_number", $"%{taxNumber}%"); }
            if (isActive.HasValue) { sql += " AND is_active=@is_active"; cmd.Parameters.AddWithValue("@is_active", isActive.Value); }

            cmd.CommandText = sql;
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return (int)Math.Ceiling((double)count / pageSize);
        }

        private Supplier MapSupplier(MySqlDataReader reader) => new Supplier
        {
            Id = reader.GetInt32("id"),
            Name = reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString("name"),
            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
            Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address"),
            TaxNumber = reader.IsDBNull(reader.GetOrdinal("tax_number")) ? null : reader.GetString("tax_number"),
            IsActive = reader.GetBoolean("is_active")
        };
    }
}
