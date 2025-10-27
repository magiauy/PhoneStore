using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class CustomerRepository : PersonRepository, ICustomerRepository
    {
        private readonly DataSource _dataSource;

        public CustomerRepository(DataSource dataSource) : base(dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public override Person GetById(int id)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    LEFT JOIN customers c ON p.id = c.person_id
                                    WHERE p.id = @id AND p.person_type = 'CUSTOMER'
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapCustomer(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting customer by ID: {id}", ex);
            }

            return null!;
        }

        public override IEnumerable<Person> GetAll()
        {
            var customers = new List<Person>();

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    LEFT JOIN customers c ON p.id = c.person_id
                                    WHERE p.person_type = 'CUSTOMER'
                                    ORDER BY p.full_name;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    customers.Add(MapCustomer(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting all customers", ex);
            }

            return customers;
        }

        public override Person GetByEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null!;

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    LEFT JOIN customers c ON p.id = c.person_id
                                    WHERE p.email = @email AND p.person_type = 'CUSTOMER'
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapCustomer(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting customer by email: {email}", ex);
            }

            return null!;
        }

        public override Person GetByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null!;

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    LEFT JOIN customers c ON p.id = c.person_id
                                    WHERE p.phone = @phone AND p.person_type = 'CUSTOMER'
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@phone", phone);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapCustomer(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting customer by phone: {phone}", ex);
            }

            return null!;
        }

        public Customer? GetByAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    INNER JOIN customers c ON p.id = c.person_id
                                    WHERE c.address = @address AND p.person_type = 'CUSTOMER'
                                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@address", address);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapCustomer(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting customer by address: {address}", ex);
            }

            return null;
        }

        public async Task<Customer?> GetByAddressAsync(string address)
        {
            return await Task.Run(() => GetByAddress(address));
        }

        public async Task<List<Customer>?> GetAllCustomersAsync()
        {
            var customers = new List<Customer>();

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type,
                                           p.created_at, p.is_active, c.address
                                    FROM persons p
                                    LEFT JOIN customers c ON p.id = c.person_id
                                    WHERE p.person_type = 'CUSTOMER'
                                    ORDER BY p.full_name;";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    customers.Add(MapCustomer(reader));
                }

                return customers;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting all customers async", ex);
                return null;
            }
        }

        public override void Insert(Person entity)
        {
            if (entity is not Customer customer)
                throw new ArgumentException("Entity must be a Customer", nameof(entity));

            customer.PersonType = PersonType.CUSTOMER;

            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                var personId = InsertPerson(conn, transaction, customer);
                customer.Id = personId;

                using var customerCmd = conn.CreateCommand();
                customerCmd.Transaction = transaction;
                customerCmd.CommandText = @"INSERT INTO customers (person_id, address)
                                            VALUES (@personId, @address);";
                customerCmd.Parameters.AddWithValue("@personId", personId);
                customerCmd.Parameters.AddWithValue("@address", customer.Address ?? (object)DBNull.Value);
                customerCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Inserted customer ID: {personId}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback error */ }
                Logger.Error("Error inserting customer", ex);
                throw;
            }
        }

        public override void Update(Person entity)
        {
            if (entity is not Customer customer)
                throw new ArgumentException("Entity must be a Customer", nameof(entity));

            customer.PersonType = PersonType.CUSTOMER;

            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                UpdatePerson(conn, transaction, customer);

                using var customerCmd = conn.CreateCommand();
                customerCmd.Transaction = transaction;
                customerCmd.CommandText = @"INSERT INTO customers (person_id, address)
                                            VALUES (@personId, @address)
                                            ON DUPLICATE KEY UPDATE address = VALUES(address);";
                customerCmd.Parameters.AddWithValue("@personId", customer.Id);
                customerCmd.Parameters.AddWithValue("@address", customer.Address ?? (object)DBNull.Value);
                customerCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Updated customer ID: {customer.Id}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback error */ }
                Logger.Error($"Error updating customer ID: {customer.Id}", ex);
                throw;
            }
        }

        public override void Delete(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                using var customerCmd = conn.CreateCommand();
                customerCmd.Transaction = transaction;
                customerCmd.CommandText = "DELETE FROM customers WHERE person_id = @id;";
                customerCmd.Parameters.AddWithValue("@id", id);
                customerCmd.ExecuteNonQuery();

                using var personCmd = conn.CreateCommand();
                personCmd.Transaction = transaction;
                personCmd.CommandText = "DELETE FROM persons WHERE id = @id;";
                personCmd.Parameters.AddWithValue("@id", id);
                personCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Deleted customer ID: {id}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback error */ }
                Logger.Error($"Error deleting customer ID: {id}", ex);
                throw;
            }
        }

        #region Helpers

        private int InsertPerson(MySqlConnection conn, MySqlTransaction transaction, Customer customer)
        {
            using var personCmd = conn.CreateCommand();
            personCmd.Transaction = transaction;
            personCmd.CommandText = @"
                INSERT INTO persons (code, full_name, phone, email, person_type, created_at, is_active)
                VALUES (@code, @fullName, @phone, @email, @personType, @createdAt, @isActive);
                SELECT LAST_INSERT_ID();";
            personCmd.Parameters.AddWithValue("@code", (object?)customer.Code ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@fullName", customer.FullName);
            personCmd.Parameters.AddWithValue("@phone", (object?)customer.Phone ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@email", (object?)customer.Email ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@personType", PersonType.CUSTOMER.ToString());
            personCmd.Parameters.AddWithValue("@createdAt", customer.CreatedAt);
            personCmd.Parameters.AddWithValue("@isActive", customer.IsActive);

            var result = personCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert customer person record.");

            return Convert.ToInt32(result);
        }

        private void UpdatePerson(MySqlConnection conn, MySqlTransaction transaction, Customer customer)
        {
            using var personCmd = conn.CreateCommand();
            personCmd.Transaction = transaction;
            personCmd.CommandText = @"
                UPDATE persons
                SET code = @code,
                    full_name = @fullName,
                    phone = @phone,
                    email = @email,
                    person_type = @personType,
                    created_at = @createdAt,
                    is_active = @isActive
                WHERE id = @id;";
            personCmd.Parameters.AddWithValue("@code", (object?)customer.Code ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@fullName", customer.FullName);
            personCmd.Parameters.AddWithValue("@phone", (object?)customer.Phone ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@email", (object?)customer.Email ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@personType", PersonType.CUSTOMER.ToString());
            personCmd.Parameters.AddWithValue("@createdAt", customer.CreatedAt);
            personCmd.Parameters.AddWithValue("@isActive", customer.IsActive);
            personCmd.Parameters.AddWithValue("@id", customer.Id);

            personCmd.ExecuteNonQuery();
        }

        private Customer MapCustomer(MySqlDataReader reader)
        {
            var customer = new Customer
            {
                Id = reader.GetInt32("id"),
                Code = reader.IsDBNull(reader.GetOrdinal("code")) ? null : reader.GetString("code"),
                FullName = reader.GetString("full_name"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                PersonType = PersonType.CUSTOMER,
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at"))
                    ? DateTime.UtcNow
                    : reader.GetDateTime("created_at"),
                IsActive = !reader.IsDBNull(reader.GetOrdinal("is_active")) && reader.GetBoolean("is_active"),
                Address = reader.IsDBNull(reader.GetOrdinal("address")) ? null : reader.GetString("address")
            };

            return customer;
        }

        #endregion
    }
}
