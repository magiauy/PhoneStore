using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MySqlConnector;
using System.Data;
using Microsoft.Extensions.Logging;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DataSource _dataSource;

        public AuthRepository(DataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public async Task<Account?> AuthenticateAsync(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, username, password_hash, person_id, is_active, created_at, last_login
                    FROM Accounts
                    WHERE username = @username AND is_active = 1
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var account = new Account
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("username"),
                        PasswordHash = reader.GetString("password_hash"),
                        PersonId = reader.GetInt32("person_id"),
                        IsActive = reader.GetBoolean("is_active"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        LastLogin = reader.IsDBNull("last_login") ? null : reader.GetDateTime("last_login")
                    };

                    if (VerifyPassword(password, account.PasswordHash))
                    {
                        await UpdateLastLoginAsync(account.Id);
                        return account;
                    }
                }

                return null;
            }
            catch (Exception)
            {
                // log exception
                return null;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            return await AuthenticateAsync(username, password) != null;
        }

        public async Task<Account?> GetAccountByUsernameAsync(string username)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, username, password_hash, person_id, is_active, created_at, last_login
                    FROM Accounts
                    WHERE Username = @username;";
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Account
                    {
                        Id = reader.GetInt32("id"),
                        Username = reader.GetString("username"),
                        PasswordHash = reader.GetString("password_hash"),
                        PersonId = reader.GetInt32("person_id"),
                        IsActive = reader.GetBoolean("is_active"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        LastLogin = reader.IsDBNull("last_login") ? null : reader.GetDateTime("last_login")
                    };
                }

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int accountId)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Accounts SET last_login = @lastLogin WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@lastLogin", DateTime.Now);
                cmd.Parameters.AddWithValue("@id", accountId);

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> IsAccountActiveAsync(int accountId)
        {
            try
            {
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT is_active FROM Accounts WHERE Id = @id;";
                cmd.Parameters.AddWithValue("@id", accountId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && Convert.ToBoolean(result);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<Account?> CreateAccountAsync(string username, string password, int personId)
        {
            Logger.Info($"Repository Creating account for user: {username}");

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                return null;

            try
            {
                // check existing
                var existing = await GetAccountByUsernameAsync(username);
                Logger.Info($"Repository Checking existing account for user: {username}");
                if (existing != null) return null;

                var hash = HashPassword(password);
                Logger.Info($"Repository Creating account for user: {username}");
                using var conn = _dataSource.GetConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Accounts (username, password_hash, person_id, is_active, created_at)
                    VALUES (@username, @hash, @personId, 1, @createdAt);
                    SELECT LAST_INSERT_ID();";
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@hash", hash);
                cmd.Parameters.AddWithValue("@personId", personId);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.Now);
                Logger.Info($"Query: {cmd.CommandText}");
                var id = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                Logger.Info($"Repository Created account ID {id} for user: {username}");
                return new Account
                {
                    Id = id,
                    Username = username,
                    PasswordHash = hash,
                    PersonId = personId,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Repository Failed to create account for user: {username}");
                Logger.Error("Exception: ", ex);
                return null;
            }
        }

        #region Helpers

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        private bool VerifyPassword(string password, string hash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash))
                return false;

            var computedHash = HashPassword(password);
            return computedHash.Equals(hash, StringComparison.Ordinal);
        }

        public Task<Account?> GetAccountWithRolesAsync(int accountId)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
