using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly DataSource _dataSource;

        public PermissionRepository(DataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        #region Synchronous Methods (Legacy)

        public Permission GetById(int id)
        {
            return GetByIdAsync(id).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Permission with ID {id} not found");
        }

        public IEnumerable<Permission> GetAll()
        {
            throw new NotImplementedException("Use async version instead");
        }

        public void Insert(Permission entity)
        {
            AddAsync(entity).GetAwaiter().GetResult();
        }

        public void Update(Permission entity)
        {
            UpdateAsync(entity).GetAwaiter().GetResult();
        }

        public void Delete(int id)
        {
            DeleteAsync(id).GetAwaiter().GetResult();
        }

        public Permission GetByCode(string code)
        {
            return GetByCodeAsync(code).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Permission with code {code} not found");
        }

        #endregion

        #region Async Methods

        public async Task<Permission?> GetByIdAsync(int id)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, code, description
                    FROM Permissions
                    WHERE id = @id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Permission
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Code = reader.GetString(reader.GetOrdinal("code")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permission by ID: {id}", ex);
                return null;
            }
        }

        public async Task<Permission?> GetByCodeAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, code, description
                    FROM Permissions
                    WHERE code = @code
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@code", code);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Permission
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Code = reader.GetString(reader.GetOrdinal("code")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permission by code: {code}", ex);
                return null;
            }
        }

        public async Task<List<Permission>?> GetAllAsync()
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, code, description
                    FROM Permissions
                    ORDER BY code ASC;";

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

                Logger.Info($"Successfully retrieved {permissions.Count} permissions");
                return permissions;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all permissions", ex);
                return null;
            }
        }

        public async Task<Permission?> AddAsync(Permission entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (string.IsNullOrWhiteSpace(entity.Code))
                    throw new ArgumentException("Permission code cannot be empty");

                // Check if permission code already exists
                var existing = await GetByCodeAsync(entity.Code);
                if (existing != null)
                {
                    Logger.Warning($"Permission with code '{entity.Code}' already exists");
                    return null;
                }

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Permissions (code, description)
                    VALUES (@code, @description);
                    SELECT LAST_INSERT_ID();";

                cmd.Parameters.AddWithValue("@code", entity.Code);
                cmd.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);

                var permissionId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                entity.Id = permissionId;

                Logger.Info($"Successfully added permission '{entity.Code}' with ID: {permissionId}");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to add permission", ex);
                return null;
            }
        }

        public async Task UpdateAsync(Permission entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (string.IsNullOrWhiteSpace(entity.Code))
                    throw new ArgumentException("Permission code cannot be empty");

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Permissions
                    SET code = @code,
                        description = @description
                    WHERE id = @id;";

                cmd.Parameters.AddWithValue("@id", entity.Id);
                cmd.Parameters.AddWithValue("@code", entity.Code);
                cmd.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully updated permission with ID: {entity.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update permission with ID: {entity.Id}", ex);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();

                // Check if permission is being used by any roles
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Role_Permissions WHERE permission_id = @permissionId;";
                checkCmd.Parameters.AddWithValue("@permissionId", id);

                var usageCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (usageCount > 0)
                {
                    throw new InvalidOperationException($"Cannot delete permission with ID {id} because it is assigned to {usageCount} role(s)");
                }

                // Delete permission
                cmd.CommandText = "DELETE FROM Permissions WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully deleted permission with ID: {id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete permission with ID: {id}", ex);
                throw;
            }
        }

        #endregion
    }
}
