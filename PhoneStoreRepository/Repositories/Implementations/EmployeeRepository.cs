using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class EmployeeRepository : PersonRepository, IEmployeeRepository
    {
        private readonly DataSource _dataSource;

        public EmployeeRepository(DataSource dataSource) : base(dataSource)
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
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    WHERE p.id = @id AND p.person_type = 'EMPLOYEE' LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapEmployee(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting employee by ID: {id}", ex);
            }

            return null!;
        }

        public override IEnumerable<Person> GetAll()
        {
            var list = new List<Person>();
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    WHERE p.person_type = 'EMPLOYEE';";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapEmployee(reader));
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting all employees", ex);
            }

            return list;
        }

        public override Person GetByEmail(string email)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    WHERE p.email = @email AND p.person_type = 'EMPLOYEE' LIMIT 1;";
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapEmployee(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting employee by email: {email}", ex);
            }

            return null!;
        }

        public override Person GetByPhone(string phone)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    WHERE p.phone = @phone AND p.person_type = 'EMPLOYEE' LIMIT 1;";
                cmd.Parameters.AddWithValue("@phone", phone);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapEmployee(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting employee by phone: {phone}", ex);
            }

            return null!;
        }

        public Employee? GetByHireDate(DateTime hireDate)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    INNER JOIN employees e ON p.id = e.person_id
                                    WHERE e.hire_date = @hireDate AND p.person_type = 'EMPLOYEE' LIMIT 1;";
                cmd.Parameters.AddWithValue("@hireDate", hireDate);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapEmployee(reader);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error getting employee by hire date: {hireDate}", ex);
            }

            return null;
        }

        public async Task<Employee?> GetByHireDateAsync(DateTime hireDate)
        {
            return await Task.Run(() => GetByHireDate(hireDate));
        }

        public async Task<List<Employee>?> GetAllEmployeesAsync()
        {
            var list = new List<Employee>();
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    WHERE p.person_type = 'EMPLOYEE'
                                    ORDER BY p.full_name;";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapEmployee(reader));
                }

                return list;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting all employees async", ex);
                return null;
            }
        }

        public async Task<List<Employee>?> GetEmployeesWithoutAccountAsync()
        {
            var list = new List<Employee>();
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT p.id, p.code, p.full_name, p.phone, p.email, p.person_type, 
                                    p.created_at, p.is_active, e.hire_date
                                    FROM persons p
                                    LEFT JOIN employees e ON p.id = e.person_id
                                    LEFT JOIN accounts a ON p.id = a.person_id
                                    WHERE p.person_type = 'EMPLOYEE' AND a.id IS NULL
                                    ORDER BY p.full_name;";

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    list.Add(MapEmployee(reader));
                }

                Logger.Info($"Found {list.Count} employees without accounts");
                return list;
            }
            catch (Exception ex)
            {
                Logger.Error("Error getting employees without accounts", ex);
                return null;
            }
        }

        public override void Insert(Person entity)
        {
            if (entity is not Employee employee)
                throw new ArgumentException("Entity must be an Employee", nameof(entity));

            employee.PersonType = PersonType.EMPLOYEE;

            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                var personId = InsertPerson(conn, transaction, employee);
                employee.Id = personId;

                using var employeeCmd = conn.CreateCommand();
                employeeCmd.Transaction = transaction;
                employeeCmd.CommandText = @"INSERT INTO employees (person_id, hire_date)
                                            VALUES (@personId, @hireDate);";
                employeeCmd.Parameters.AddWithValue("@personId", personId);
                employeeCmd.Parameters.AddWithValue("@hireDate", employee.HireDate.HasValue
                    ? employee.HireDate.Value.Date
                    : (object)DBNull.Value);
                employeeCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Inserted employee ID: {personId}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                Logger.Error("Error inserting employee", ex);
                throw;
            }
        }

        public override void Update(Person entity)
        {
            if (entity is not Employee employee)
                throw new ArgumentException("Entity must be an Employee", nameof(entity));

            employee.PersonType = PersonType.EMPLOYEE;

            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                UpdatePerson(conn, transaction, employee);

                using var employeeCmd = conn.CreateCommand();
                employeeCmd.Transaction = transaction;
                employeeCmd.CommandText = @"INSERT INTO employees (person_id, hire_date)
                                            VALUES (@personId, @hireDate)
                                            ON DUPLICATE KEY UPDATE hire_date = VALUES(hire_date);";
                employeeCmd.Parameters.AddWithValue("@personId", employee.Id);
                employeeCmd.Parameters.AddWithValue("@hireDate", employee.HireDate.HasValue
                    ? employee.HireDate.Value.Date
                    : (object)DBNull.Value);
                employeeCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Updated employee ID: {employee.Id}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                Logger.Error($"Error updating employee ID: {employee.Id}", ex);
                throw;
            }
        }

        public override void Delete(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var transaction = conn.BeginTransaction();

            try
            {
                using var employeeCmd = conn.CreateCommand();
                employeeCmd.Transaction = transaction;
                employeeCmd.CommandText = "DELETE FROM employees WHERE person_id = @id;";
                employeeCmd.Parameters.AddWithValue("@id", id);
                employeeCmd.ExecuteNonQuery();

                using var personCmd = conn.CreateCommand();
                personCmd.Transaction = transaction;
                personCmd.CommandText = "DELETE FROM persons WHERE id = @id;";
                personCmd.Parameters.AddWithValue("@id", id);
                personCmd.ExecuteNonQuery();

                transaction.Commit();
                Logger.Info($"Deleted employee ID: {id}");
            }
            catch (Exception ex)
            {
                try { transaction.Rollback(); } catch { /* ignore rollback errors */ }
                Logger.Error($"Error deleting employee ID: {id}", ex);
                throw;
            }
        }

        #region Helpers

        private int InsertPerson(MySqlConnection conn, MySqlTransaction transaction, Employee employee)
        {
            using var personCmd = conn.CreateCommand();
            personCmd.Transaction = transaction;
            personCmd.CommandText = @"
                INSERT INTO persons (code, full_name, phone, email, person_type, created_at, is_active)
                VALUES (@code, @fullName, @phone, @email, @personType, @createdAt, @isActive);
                SELECT LAST_INSERT_ID();";
            personCmd.Parameters.AddWithValue("@code", (object?)employee.Code ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@fullName", employee.FullName);
            personCmd.Parameters.AddWithValue("@phone", (object?)employee.Phone ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@email", (object?)employee.Email ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@personType", PersonType.EMPLOYEE.ToString());
            personCmd.Parameters.AddWithValue("@createdAt", employee.CreatedAt);
            personCmd.Parameters.AddWithValue("@isActive", employee.IsActive);

            var result = personCmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Failed to insert employee person record.");

            return Convert.ToInt32(result);
        }

        private void UpdatePerson(MySqlConnection conn, MySqlTransaction transaction, Employee employee)
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
            personCmd.Parameters.AddWithValue("@code", (object?)employee.Code ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@fullName", employee.FullName);
            personCmd.Parameters.AddWithValue("@phone", (object?)employee.Phone ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@email", (object?)employee.Email ?? DBNull.Value);
            personCmd.Parameters.AddWithValue("@personType", PersonType.EMPLOYEE.ToString());
            personCmd.Parameters.AddWithValue("@createdAt", employee.CreatedAt);
            personCmd.Parameters.AddWithValue("@isActive", employee.IsActive);
            personCmd.Parameters.AddWithValue("@id", employee.Id);

            personCmd.ExecuteNonQuery();
        }

        private Employee MapEmployee(MySqlDataReader reader)
        {
            var employee = new Employee
            {
                Id = reader.GetInt32("id"),
                Code = reader.IsDBNull(reader.GetOrdinal("code")) ? null : reader.GetString("code"),
                FullName = reader.GetString("full_name"),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString("phone"),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString("email"),
                PersonType = PersonType.EMPLOYEE,
                CreatedAt = reader.IsDBNull(reader.GetOrdinal("created_at")) ? DateTime.Now : reader.GetDateTime("created_at"),
                IsActive = reader.GetBoolean("is_active"),
                HireDate = reader.IsDBNull(reader.GetOrdinal("hire_date")) ? null : reader.GetDateTime("hire_date")
            };

            return employee;
        }

        #endregion
    }
}
