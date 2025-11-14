using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class AccountRepository(DataSource dataSource) : IAccountRepository
    {
        private readonly DataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));


        #region Synchronous Methods (Legacy)
        
        public Account GetById(int id)
        {
            return GetByIdAsync(id).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Account with ID {id} not found");
        }

        public IEnumerable<Account> GetAll()
        {
            throw new NotImplementedException("Use async version instead");
        }

        public void Insert(Account entity)
        {
            AddAsync(entity).GetAwaiter().GetResult();
        }

        public void Update(Account entity)
        {
            UpdateAsync(entity).GetAwaiter().GetResult();
        }

        public void Delete(int id)
        {
            DeleteAsync(id).GetAwaiter().GetResult();
        }

        public Account GetByUsername(string username)
        {
            return GetByUsernameAsync(username).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Account with username {username} not found");
        }

        #endregion

        #region Async Methods

        public async Task<Account?> GetByIdAsync(int id)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, username, password_hash, person_id, is_active, created_at, last_login
                    FROM accounts
                    WHERE id = @id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Account
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                        PersonId = reader.GetInt32(reader.GetOrdinal("person_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedAt = TryGetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                        LastLogin = TryGetDateTime(reader, "last_login")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account by ID: {id}", ex);
                return null;
            }
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, username, password_hash, person_id, is_active, created_at, last_login
                    FROM Accounts
                    WHERE username = @username
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@username", username);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Account
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                        PersonId = reader.GetInt32(reader.GetOrdinal("person_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedAt = TryGetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                        LastLogin = TryGetDateTime(reader, "last_login")
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account by username: {username}", ex);
                return null;
            }
        }

        public async Task<Account?> GetWithRolesAsync(int accountId)
        {
            try
            {
                var account = await GetByIdAsync(accountId);
                if (account == null)
                    return null;

                // Get roles for this account
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT r.id, r.name, r.description, r.weight
                    FROM Roles r
                    INNER JOIN Account_Roles ar ON r.id = ar.role_id
                    WHERE ar.account_id = @accountId;";
                cmd.Parameters.AddWithValue("@accountId", accountId);

                await using var reader = await cmd.ExecuteReaderAsync();
                var accountRoles = new List<AccountRole>();
                
                while (await reader.ReadAsync())
                {
                    var role = new Role
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Weight = reader.GetInt32(reader.GetOrdinal("weight"))
                    };

                    accountRoles.Add(new AccountRole
                    {
                        AccountId = accountId,
                        RoleId = role.Id,
                        Role = role
                    });
                }

                account.AccountRoles = accountRoles;
                return account;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account with roles for ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<List<Role>?> GetRolesByAccountIdAsync(int accountId)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT r.id, r.name, r.description, r.weight
                    FROM Roles r
                    INNER JOIN Account_Roles ar ON r.id = ar.role_id
                    WHERE ar.account_id = @accountId;";
                cmd.Parameters.AddWithValue("@accountId", accountId);

                await using var reader = await cmd.ExecuteReaderAsync();
                var roles = new List<Role>();

                while (await reader.ReadAsync())
                {
                    roles.Add(new Role
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Weight = reader.GetInt32(reader.GetOrdinal("weight"))
                    });
                }

                return roles;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get roles for account ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<List<Permission>?> GetPermissionsByAccountIdAsync(int accountId)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT DISTINCT p.id, p.code, p.description
                    FROM Permissions p
                    INNER JOIN Role_Permissions rp ON p.id = rp.permission_id
                    INNER JOIN Account_Roles ar ON rp.role_id = ar.role_id
                    WHERE ar.account_id = @accountId;";
                cmd.Parameters.AddWithValue("@accountId", accountId);

                await using var reader = await cmd.ExecuteReaderAsync();
                var permissions = new List<Permission>();

                while (await reader.ReadAsync())
                {
                    permissions.Add(new Permission
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Code = reader.GetString(reader.GetOrdinal("code")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"))
                    });
                }

                return permissions;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permissions for account ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<bool> IsActiveAsync(int accountId)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT is_active FROM Accounts WHERE id = @id LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", accountId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null && Convert.ToBoolean(result);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check if account is active for ID: {accountId}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int accountId)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Accounts SET last_login = @lastLogin WHERE id = @id;";
                cmd.Parameters.AddWithValue("@lastLogin", DateTime.Now);
                cmd.Parameters.AddWithValue("@id", accountId);

                return await cmd.ExecuteNonQueryAsync() > 0;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update last login for account ID: {accountId}", ex);
                return false;
            }
        }

        public async Task<Account?> AddAsync(Account entity)
        {
            MySqlConnection? conn = null;
            MySqlTransaction? transaction = null;
            
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (entity.Person == null)
                    throw new ArgumentException("Account must have Person information");

                // Check if username already exists
                var existing = await GetByUsernameAsync(entity.Username);
                if (existing != null)
                {
                    Logger.Warning($"Account with username {entity.Username} already exists");
                    return null;
                }

                conn = _dataSource.GetConnection();
                transaction = await conn.BeginTransactionAsync();

                // Insert Person first
                await using var personCmd = conn.CreateCommand();
                personCmd.Transaction = transaction;
                personCmd.CommandText = @"
                    INSERT INTO Persons (full_name, email, phone, person_type, created_at, is_active)
                    VALUES (@fullName, @email, @phone, @personType, @createdAt, @isActive);
                    SELECT LAST_INSERT_ID();";
                
                personCmd.Parameters.AddWithValue("@fullName", entity.Person.FullName);
                personCmd.Parameters.AddWithValue("@email", entity.Person.Email ?? (object)DBNull.Value);
                personCmd.Parameters.AddWithValue("@phone", entity.Person.Phone ?? (object)DBNull.Value);
                personCmd.Parameters.AddWithValue("@personType", entity.Person.PersonType.ToString());
                personCmd.Parameters.AddWithValue("@createdAt", entity.Person.CreatedAt);
                personCmd.Parameters.AddWithValue("@isActive", entity.Person.IsActive);

                var personId = Convert.ToInt32(await personCmd.ExecuteScalarAsync());
                entity.Person.Id = personId;
                entity.PersonId = personId;
                
                Logger.Info($"Created Person with ID: {personId}, Type: {entity.Person.PersonType}");

                // Hash password using PasswordHasher utility
                var passwordHash = PasswordHasher.HashPassword(entity.PasswordHash);

                // Insert Account
                await using var accountCmd = conn.CreateCommand();
                accountCmd.Transaction = transaction;
                accountCmd.CommandText = @"
                    INSERT INTO Accounts (username, password_hash, person_id, is_active, created_at)
                    VALUES (@username, @passwordHash, @personId, @isActive, @createdAt);
                    SELECT LAST_INSERT_ID();";
                
                accountCmd.Parameters.AddWithValue("@username", entity.Username);
                accountCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                accountCmd.Parameters.AddWithValue("@personId", personId);
                accountCmd.Parameters.AddWithValue("@isActive", entity.IsActive);
                accountCmd.Parameters.AddWithValue("@createdAt", entity.CreatedAt);

                var accountId = Convert.ToInt32(await accountCmd.ExecuteScalarAsync());
                entity.Id = accountId;

                await transaction.CommitAsync();
                
                Logger.Info($"Successfully added account '{entity.Username}' with ID: {accountId}, Person ID: {personId}");
                return entity;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    Logger.Warning("Transaction rolled back due to error");
                }
                Logger.Error("Failed to add account", ex);
                return null;
            }
            finally
            {
                transaction?.Dispose();
                conn?.Dispose();
            }
        }


        public async Task UpdateAsync(Account entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Accounts 
                    SET username = @username,
                        password_hash = @passwordHash,
                        person_id = @personId,
                        is_active = @isActive,
                        last_login = @lastLogin
                    WHERE id = @id;";
                
                cmd.Parameters.AddWithValue("@id", entity.Id);
                cmd.Parameters.AddWithValue("@username", entity.Username);
                cmd.Parameters.AddWithValue("@passwordHash", entity.PasswordHash);
                cmd.Parameters.AddWithValue("@personId", entity.PersonId);
                cmd.Parameters.AddWithValue("@isActive", entity.IsActive);
                cmd.Parameters.AddWithValue("@lastLogin", entity.LastLogin.HasValue ? (object)entity.LastLogin.Value : DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully updated account with ID: {entity.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update account with ID: {entity.Id}", ex);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                
                // Soft delete by setting is_active to false
                cmd.CommandText = "UPDATE Accounts SET is_active = 0 WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully deleted (soft delete) account with ID: {id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete account with ID: {id}", ex);
                throw;
            }
        }

        public async Task<List<Account>?> GetAllAsync()
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = """
                    SELECT a.id, a.username, a.password_hash, a.person_id, a.is_active, a.created_at, a.last_login,
                           p.full_name, p.person_type, p.email, p.phone
                    FROM Accounts a
                    INNER JOIN Persons p ON a.person_id = p.id
                    ORDER BY a.created_at DESC;
                    """;

                await using var reader = await cmd.ExecuteReaderAsync();
                var accounts = new List<Account>();

                while (await reader.ReadAsync())
                {
                    var account = new Account
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Username = reader.GetString(reader.GetOrdinal("username")),
                        PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                        PersonId = reader.GetInt32(reader.GetOrdinal("person_id")),
                        IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                        CreatedAt = TryGetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                        LastLogin = TryGetDateTime(reader, "last_login"),
                        Person = new Person
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("person_id")),
                            FullName = reader.GetString(reader.GetOrdinal("full_name")),
                            PersonType = Enum.TryParse<Models.Enums.PersonType>(reader.GetString(reader.GetOrdinal("person_type")), out var pt) ? pt : default,
                            Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                            Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone"))
                        }
                    };

                    accounts.Add(account);
                }

                Logger.Info($"Successfully retrieved {accounts.Count} accounts");
                return accounts;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all accounts", ex);
                return null;
            }
        }

        public async Task<(List<Account> Accounts, int TotalCount)?> GetPagedAsync(int pageIndex, int pageSize, string? searchText = null, AccountFilterCriteria? filterCriteria = null)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                
                // Build WHERE clause dynamically based on filters
                var whereClauses = new List<string>();
                var parameters = new Dictionary<string, object>();
                
                // Search filter
                var hasSearch = !string.IsNullOrWhiteSpace(searchText);
                if (hasSearch)
                {
                    whereClauses.Add("(a.username LIKE @search OR p.full_name LIKE @search)");
                    parameters["@search"] = $"%{searchText}%";
                }

                // Apply filter criteria if provided
                if (filterCriteria != null && filterCriteria.HasAnyFilter())
                {
                    // Status filter: Activated / Deactivated
                    if (filterCriteria.Status == "Activated")
                    {
                        whereClauses.Add("a.is_active = 1");
                    }
                    else if (filterCriteria.Status == "Deactivated")
                    {
                        whereClauses.Add("a.is_active = 0");
                    }

                    // Account Type filter: Employee / Customer
                    if (filterCriteria.AccountType == "Employee")
                    {
                        whereClauses.Add("p.person_type = 'EMPLOYEE'");
                    }
                    else if (filterCriteria.AccountType == "Customer")
                    {
                        whereClauses.Add("p.person_type = 'CUSTOMER'");
                    }

                    // Created date range filter
                    if (filterCriteria.CreatedFrom.HasValue)
                    {
                        whereClauses.Add("DATE(a.created_at) >= @createdFrom");
                        parameters["@createdFrom"] = filterCriteria.CreatedFrom.Value.Date;
                    }
                    if (filterCriteria.CreatedTo.HasValue)
                    {
                        whereClauses.Add("DATE(a.created_at) <= @createdTo");
                        parameters["@createdTo"] = filterCriteria.CreatedTo.Value.Date;
                    }

                    // Last login filters
                    if (filterCriteria.NeverLoggedIn)
                    {
                        whereClauses.Add("(a.last_login IS NULL OR a.last_login = '0000-00-00 00:00:00')");
                    }
                    else
                    {
                        // Last login date range (only if not filtering for "never logged in")
                        if (filterCriteria.LastLoginFrom.HasValue)
                        {
                            whereClauses.Add("DATE(a.last_login) >= @lastLoginFrom");
                            parameters["@lastLoginFrom"] = filterCriteria.LastLoginFrom.Value.Date;
                        }
                        if (filterCriteria.LastLoginTo.HasValue)
                        {
                            whereClauses.Add("DATE(a.last_login) <= @lastLoginTo");
                            parameters["@lastLoginTo"] = filterCriteria.LastLoginTo.Value.Date;
                        }
                    }
                }

                // Combine WHERE clauses
                var whereClause = whereClauses.Count > 0 
                    ? "WHERE " + string.Join(" AND ", whereClauses)
                    : "";

                // Get total count with filters
                int totalCount;
                await using (var countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = $"""
                        SELECT COUNT(*)
                        FROM Accounts a
                        INNER JOIN Persons p ON a.person_id = p.id
                        {whereClause};
                        """;
                    
                    foreach (var param in parameters)
                    {
                        countCmd.Parameters.AddWithValue(param.Key, param.Value);
                    }
                    
                    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
                }

                // Get paged data with filters
                var offset = (pageIndex - 1) * pageSize;
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = $"""
                        SELECT 
                            a.id, a.username, a.password_hash, a.person_id, 
                            a.is_active, a.created_at, a.last_login,
                            p.full_name, p.person_type, p.email, p.phone
                        FROM Accounts a
                        INNER JOIN Persons p ON a.person_id = p.id
                        {whereClause}
                        ORDER BY a.id DESC
                        LIMIT @limit OFFSET @offset;
                        """;
                    
                    cmd.Parameters.AddWithValue("@limit", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);
                    
                    foreach (var param in parameters)
                    {
                        cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    await using var reader = await cmd.ExecuteReaderAsync();
                    var accounts = new List<Account>();

                    while (await reader.ReadAsync())
                    {
                        var account = new Account
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("id")),
                            Username = reader.GetString(reader.GetOrdinal("username")),
                            PasswordHash = reader.GetString(reader.GetOrdinal("password_hash")),
                            PersonId = reader.GetInt32(reader.GetOrdinal("person_id")),
                            IsActive = reader.GetBoolean(reader.GetOrdinal("is_active")),
                            CreatedAt = TryGetDateTime(reader, "created_at") ?? DateTime.UtcNow,
                            LastLogin = TryGetDateTime(reader, "last_login"),
                            Person = new Person
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("person_id")),
                                FullName = reader.GetString(reader.GetOrdinal("full_name")),
                                PersonType = Enum.TryParse<PhoneStoreRepository.Models.Enums.PersonType>(reader.GetString(reader.GetOrdinal("person_type")), out var pt) ? pt : default,
                                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? null : reader.GetString(reader.GetOrdinal("email")),
                                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? null : reader.GetString(reader.GetOrdinal("phone"))
                            }
                        };

                        accounts.Add(account);
                    }

                    Logger.Info($"Successfully retrieved {accounts.Count} accounts with filters (page {pageIndex}, total: {totalCount})");
                    return (accounts, totalCount);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get paged accounts (page: {pageIndex}, size: {pageSize})", ex);
                return null;
            }
        }

        public async Task UpdateRolesAsync(int accountId, IEnumerable<int> roleIds)
        {
            await using var conn = _dataSource.GetConnection();
            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                var roles = roleIds?.Distinct().ToList() ?? new List<int>();

                await using (var deleteCmd = conn.CreateCommand())
                {
                    deleteCmd.Transaction = transaction;
                    deleteCmd.CommandText = "DELETE FROM Account_Roles WHERE account_id = @accountId;";
                    deleteCmd.Parameters.AddWithValue("@accountId", accountId);
                    await deleteCmd.ExecuteNonQueryAsync();
                }

                foreach (var roleId in roles)
                {
                    await using var insertCmd = conn.CreateCommand();
                    insertCmd.Transaction = transaction;
                    insertCmd.CommandText = "INSERT INTO Account_Roles (account_id, role_id) VALUES (@accountId, @roleId);";
                    insertCmd.Parameters.AddWithValue("@accountId", accountId);
                    insertCmd.Parameters.AddWithValue("@roleId", roleId);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                Logger.Info($"Updated roles for account ID {accountId} with {roles.Count} entries.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Logger.Error($"Failed to update roles for account ID: {accountId}", ex);
                throw;
            }
        }

        private static DateTime? TryGetDateTime(MySqlDataReader reader, string columnName)
        {
            var ordinal = reader.GetOrdinal(columnName);
            if (reader.IsDBNull(ordinal))
                return null;

            try
            {
                return reader.GetDateTime(ordinal);
            }
            catch
            {
                // Includes zero date/time values or malformed data
                return null;
            }
        }

        #endregion
    }
}
