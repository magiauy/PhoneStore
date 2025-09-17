using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using MySqlConnector;
using System.Data;

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
                cmd.CommandText = @"SELECT Id, Code, FullName, Phone, Email, PersonType, CreatedAt, IsActive
                                    FROM persons
                                    WHERE Id = @id LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return MapPerson(reader);
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
                cmd.CommandText = @"SELECT Id, Code, FullName, Phone, Email, PersonType, CreatedAt, IsActive FROM persons;";

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
            cmd.CommandText = @"INSERT INTO persons (Code, FullName, Phone, Email, PersonType, CreatedAt, IsActive)
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
            cmd.CommandText = @"UPDATE persons SET Code = @code, FullName = @fullName, Phone = @phone, Email = @email,
                                PersonType = @personType, CreatedAt = @createdAt, IsActive = @isActive
                                WHERE Id = @id;";
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
            cmd.CommandText = "DELETE FROM persons WHERE Id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

    public virtual Person GetByEmail(string email)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"SELECT Id, Code, FullName, Phone, Email, PersonType, CreatedAt, IsActive
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
                cmd.CommandText = @"SELECT Id, Code, FullName, Phone, Email, PersonType, CreatedAt, IsActive
                                    FROM persons
                                    WHERE Phone = @phone LIMIT 1;";
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
            return new Person
            {
                Id = reader.GetInt32("Id"),
                Code = reader.IsDBNull("Code") ? null : reader.GetString("Code"),
                FullName = reader.GetString("FullName"),
                Phone = reader.IsDBNull("Phone") ? null : reader.GetString("Phone"),
                Email = reader.IsDBNull("Email") ? null : reader.GetString("Email"),
                PersonType = (PhoneStoreAdmin.Models.Enums.PersonType)reader.GetInt32("PersonType"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                IsActive = reader.GetBoolean("IsActive")
            };
        }

        #endregion
    }
}

