using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace PhoneStoreAdmin.Repositories.Implementations
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

        #region Helpers

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
                CreatedAt = reader.GetDateTime("created_at"),
                IsActive = reader.GetBoolean("is_active"),
                HireDate = reader.IsDBNull(reader.GetOrdinal("hire_date")) ? null : reader.GetDateTime("hire_date")
            };

            return employee;
        }

        #endregion
    }
}
