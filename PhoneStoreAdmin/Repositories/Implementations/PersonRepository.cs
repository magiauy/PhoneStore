using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using MySqlConnector;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class PersonRepository : IPersonRepository
    {
        private readonly DataSource _dataSource;

        public PersonRepository(DataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

    public virtual Person GetById(int id)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, code, full_name, phone, email, person_type, created_at, is_active
                                    FROM persons
                                    WHERE id = @id LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                try
                {
                    if (reader.Read())
                    {
                        Person p = MapPerson(reader);
                        return p;
                    }
                }catch (Exception ex)
                {
                    Logger.Error("Error reading data", ex);
                }

                return null!;
            }
            catch (Exception)
            {
                return null!;
            }
        }

    public virtual IEnumerable<Person> GetAll()
        {
            var list = new List<Person>();
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, Code, full_name, phone, email, person_type, created_at, is_active FROM persons;";

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    list.Add(MapPerson(reader));
                }
            }
            catch (Exception)
            {
                // ignore
            }

            return list;
        }

    public virtual void Insert(Person entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO persons (Code, full_name, phone, email, person_type, created_at, is_active)
                                VALUES (@code, @fullName, @phone, @email, @personType, @createdAt, @isActive);";
            cmd.Parameters.AddWithValue("@code", (object?)entity.Code ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fullName", entity.FullName);
            cmd.Parameters.AddWithValue("@phone", (object?)entity.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)entity.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@personType", (int)entity.PersonType);
            cmd.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            cmd.Parameters.AddWithValue("@isActive", entity.IsActive);

            cmd.ExecuteNonQuery();
        }

    public virtual void Update(Person entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"UPDATE persons SET Code = @code, full_name = @fullName, phone = @phone, email = @email,
                                person_type = @personType, created_at = @createdAt, is_active = @isActive
                                WHERE id = @id;";
            cmd.Parameters.AddWithValue("@code", (object?)entity.Code ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fullName", entity.FullName);
            cmd.Parameters.AddWithValue("@phone", (object?)entity.Phone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@email", (object?)entity.Email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@personType", (int)entity.PersonType);
            cmd.Parameters.AddWithValue("@createdAt", entity.CreatedAt);
            cmd.Parameters.AddWithValue("@isActive", entity.IsActive);
            cmd.Parameters.AddWithValue("@id", entity.Id);

            cmd.ExecuteNonQuery();
        }

    public virtual void Delete(int id)
        {
            using var conn = _dataSource.GetConnection();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "DELETE FROM persons WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

    public virtual Person GetByEmail(string email)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, Code, full_name, phone, email, person_type, created_at, is_active
                                    FROM persons
                                    WHERE Email = @email LIMIT 1;";
                cmd.Parameters.AddWithValue("@email", email);

                using var reader = cmd.ExecuteReader();
                if (reader.Read()) return MapPerson(reader);
            }
            catch (Exception)
            {
            }

            return null!;
        }

    public virtual Person GetByPhone(string phone)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT id, Code, full_name, phone, email, person_type, created_at, is_active
                                    FROM persons
                                    WHERE phone = @phone LIMIT 1;";
                cmd.Parameters.AddWithValue("@phone", phone);

                using var reader = cmd.ExecuteReader();
                if (reader.Read()) return MapPerson(reader);
            }
            catch (Exception)
            {
            }

            return null!;
        }

        #region Helpers

        private Person MapPerson(MySqlDataReader reader)
        {
            var person = new Person
            {
                Id = reader.GetInt32("id"),
                Code = reader.IsDBNull("code") ? null : reader.GetString("code"),
                FullName = reader.GetString("full_name"),
                Phone = reader.IsDBNull("phone") ? null : reader.GetString("phone"),
                Email = reader.IsDBNull("email") ? null : reader.GetString("email"),
                CreatedAt = reader.GetDateTime("created_at"),
                IsActive = reader.GetBoolean("is_active")
            };
        
            try
            {
                // Nếu DB trả về int
                person.PersonType = (PhoneStoreAdmin.Models.Enums.PersonType)reader.GetInt32("person_type");
            }
            catch
            {
                // Nếu DB trả về string
                var typeStr = reader.GetString("person_type");
                if (Enum.TryParse(typeStr, out PhoneStoreAdmin.Models.Enums.PersonType type))
                    person.PersonType = type;
                else
                    person.PersonType = PhoneStoreAdmin.Models.Enums.PersonType.CUSTOMER; // fallback
            }
        
            return person;
        }

        #endregion

        #region Async Methods

        public async Task<Person?> GetByIdAsync(int id)
        {
            // TODO: Implement actual async database query
            Person p = await Task.Run(() => GetById(id));
            Logger.Info($"GetByIdAsync returned: {p}");
            return await Task.FromResult(p);
        }

        public async Task<Person?> GetByAccountIdAsync(int accountId)
        {
            // TODO: Implement actual database query to get person by account ID
            return await Task.FromResult<Person?>(null);
        }

        public async Task<Person?> GetByEmailAsync(string email)
        {
            // TODO: Implement actual async database query
            return await Task.FromResult(GetByEmail(email));
        }

        public async Task<Person?> GetByPhoneAsync(string phone)
        {
            // TODO: Implement actual async database query
            return await Task.FromResult(GetByPhone(phone));
        }

        public async Task<Person?> AddAsync(Person entity)
        {
            // TODO: Implement actual async database insert
            Insert(entity);
            return await Task.FromResult(entity);
        }

        public async Task UpdateAsync(Person entity)
        {
            // TODO: Implement actual async database update
            Update(entity);
            await Task.CompletedTask;
        }

        public async Task DeleteAsync(int id)
        {
            // TODO: Implement actual async database delete
            Delete(id);
            await Task.CompletedTask;
        }

        #endregion
    }
}

