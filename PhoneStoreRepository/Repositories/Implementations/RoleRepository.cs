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
    public class RoleRepository (DataSource dataSource) : IRoleRepository
    {
        private readonly DataSource _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));

        #region Synchronous Methods (Legacy)

        public Role GetById(int id)
        {
            return GetByIdAsync(id).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Role with ID {id} not found");
        }

        public IEnumerable<Role> GetAll()
        {
            throw new NotImplementedException("Use async version instead");
        }

        public void Insert(Role entity)
        {
            AddAsync(entity).GetAwaiter().GetResult();
        }

        public void Update(Role entity)
        {
            UpdateAsync(entity).GetAwaiter().GetResult();
        }

        public void Delete(int id)
        {
            DeleteAsync(id).GetAwaiter().GetResult();
        }

        public Role GetByName(string name)
        {
            return GetByNameAsync(name).GetAwaiter().GetResult() ?? throw new InvalidOperationException($"Role with name {name} not found");
        }

        #endregion

        #region Async Methods

        public async Task<Role?> GetByIdAsync(int id)
        {
            try
            {
                Role? role = null;
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, name, description, weight
                    FROM Roles
                    WHERE id = @id
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@id", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    role = new Role
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Weight = reader.GetInt32(reader.GetOrdinal("weight"))
                    };
                }
                else
                {
                    // Role not found
                    return null;
                }
                
                // Close the first reader before executing the second query
                await reader.CloseAsync();

                cmd.CommandText = @"
                    SELECT role_id, permission_id
                    FROM Role_Permissions
                    WHERE role_id = @id;";
                await using var reader2 = await cmd.ExecuteReaderAsync();
                var rolePermissions = new List<RolePermission>();
                while (await reader2.ReadAsync())
                {
                    rolePermissions.Add(new RolePermission
                    {
                        RoleId = reader2.GetInt32(reader2.GetOrdinal("role_id")),
                        PermissionId = reader2.GetInt32(reader2.GetOrdinal("permission_id"))
                    });
                }
                role.RolePermissions = rolePermissions;
                
                Logger.Info($"Loaded role '{role.Name}' (ID: {role.Id}) with {rolePermissions.Count} permissions: [{string.Join(", ", rolePermissions.Select(rp => rp.PermissionId))}]");
                
                return role;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get role by ID: {id}", ex);
                return null;
            }
        }

        public async Task<Role?> GetByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, name, description, weight
                    FROM Roles
                    WHERE name = @name
                    LIMIT 1;";
                cmd.Parameters.AddWithValue("@name", name);

                await using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new Role
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("id")),
                        Name = reader.GetString(reader.GetOrdinal("name")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        Weight = reader.GetInt32(reader.GetOrdinal("weight"))
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get role by name: {name}", ex);
                return null;
            }
        }

        public async Task<List<Role>?> GetAllAsync()
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT id, name, description, weight
                    FROM Roles
                    ORDER BY weight ASC, name ASC;";

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

                // Populate role permissions so callers have accurate counts
                var roleIds = roles.Select(r => r.Id).ToList();
                if (roleIds.Count > 0)
                {
                    var parameterNames = roleIds.Select((_, index) => $"@roleId{index}").ToList();
                    var inClause = string.Join(",", parameterNames);

                    await reader.DisposeAsync();

                    cmd.CommandText = $"""
                        SELECT role_id, permission_id
                        FROM Role_Permissions
                        WHERE role_id IN ({inClause});
                        """;
                    cmd.Parameters.Clear();
                    for (var i = 0; i < roleIds.Count; i++)
                    {
                        cmd.Parameters.AddWithValue(parameterNames[i], roleIds[i]);
                    }

                    var permissionsLookup = roles.ToDictionary(r => r.Id, _ => new List<RolePermission>());

                    await using var permissionsReader = await cmd.ExecuteReaderAsync();
                    while (await permissionsReader.ReadAsync())
                    {
                        var roleId = permissionsReader.GetInt32(permissionsReader.GetOrdinal("role_id"));
                        var permissionId = permissionsReader.GetInt32(permissionsReader.GetOrdinal("permission_id"));

                        if (permissionsLookup.TryGetValue(roleId, out var list))
                        {
                            list.Add(new RolePermission
                            {
                                RoleId = roleId,
                                PermissionId = permissionId
                            });
                        }
                    }

                    foreach (var role in roles)
                    {
                        if (permissionsLookup.TryGetValue(role.Id, out var list))
                        {
                            role.RolePermissions = list;
                        }
                    }
                }

                Logger.Info($"Successfully retrieved {roles.Count} roles");
                return roles;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all roles", ex);
                return null;
            }
        }

        public async Task<Role?> AddAsync(Role entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (string.IsNullOrWhiteSpace(entity.Name))
                    throw new ArgumentException("Role name cannot be empty");

                // Check if role name already exists
                var existing = await GetByNameAsync(entity.Name);
                if (existing != null)
                {
                    Logger.Warning($"Role with name '{entity.Name}' already exists");
                    return null;
                }

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO Roles (name, description, weight)
                    VALUES (@name, @description, @weight);
                    SELECT LAST_INSERT_ID();";

                cmd.Parameters.AddWithValue("@name", entity.Name);
                cmd.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@weight", entity.Weight);

                var roleId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                entity.Id = roleId;

                Logger.Info($"Successfully added role '{entity.Name}' with ID: {roleId}");
                return entity;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to add role", ex);
                return null;
            }
        }

        public async Task UpdateAsync(Role entity)
        {
            try
            {
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                if (string.IsNullOrWhiteSpace(entity.Name))
                    throw new ArgumentException("Role name cannot be empty");

                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    UPDATE Roles
                    SET name = @name,
                        description = @description,
                        weight = @weight
                    WHERE id = @id;";

                cmd.Parameters.AddWithValue("@id", entity.Id);
                cmd.Parameters.AddWithValue("@name", entity.Name);
                cmd.Parameters.AddWithValue("@description", entity.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@weight", entity.Weight);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully updated role with ID: {entity.Id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update role with ID: {entity.Id}", ex);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();

                // Check if role is being used by any accounts
                await using var checkCmd = conn.CreateCommand();
                checkCmd.CommandText = "SELECT COUNT(*) FROM Account_Roles WHERE role_id = @roleId;";
                checkCmd.Parameters.AddWithValue("@roleId", id);

                var usageCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (usageCount > 0)
                {
                    throw new InvalidOperationException($"Cannot delete role with ID {id} because it is assigned to {usageCount} account(s)");
                }

                // Delete role
                cmd.CommandText = "DELETE FROM Roles WHERE id = @id;";
                cmd.Parameters.AddWithValue("@id", id);

                await cmd.ExecuteNonQueryAsync();
                Logger.Info($"Successfully deleted role with ID: {id}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete role with ID: {id}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            MySqlConnection? conn = null;
            MySqlTransaction? transaction = null;

            try
            {
                conn = _dataSource.GetConnection();
                transaction = await conn.BeginTransactionAsync();

                // First, remove all existing permissions for this role
                await using var deleteCmd = conn.CreateCommand();
                deleteCmd.Transaction = transaction;
                deleteCmd.CommandText = "DELETE FROM Role_Permissions WHERE role_id = @roleId;";
                deleteCmd.Parameters.AddWithValue("@roleId", roleId);
                await deleteCmd.ExecuteNonQueryAsync();

                // Then, add the new permissions
                if (permissionIds != null && permissionIds.Count > 0)
                {
                    foreach (var permissionId in permissionIds)
                    {
                        await using var insertCmd = conn.CreateCommand();
                        insertCmd.Transaction = transaction;
                        insertCmd.CommandText = @"
                            INSERT INTO Role_Permissions (role_id, permission_id)
                            VALUES (@roleId, @permissionId);";
                        insertCmd.Parameters.AddWithValue("@roleId", roleId);
                        insertCmd.Parameters.AddWithValue("@permissionId", permissionId);
                        await insertCmd.ExecuteNonQueryAsync();
                    }
                }

                await transaction.CommitAsync();
                Logger.Info($"Successfully updated permissions for role ID: {roleId} ({permissionIds?.Count ?? 0} permissions)");
                return true;
            }
            catch (Exception ex)
            {
                if (transaction != null)
                {
                    await transaction.RollbackAsync();
                    Logger.Warning("Transaction rolled back due to error");
                }
                Logger.Error($"Failed to update permissions for role ID: {roleId}", ex);
                return false;
            }
            finally
            {
                transaction?.Dispose();
                conn?.Dispose();
            }
        }

        public async Task<List<Permission>?> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                await using var conn = _dataSource.GetConnection();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT p.id, p.code, p.description
                    FROM Permissions p
                    INNER JOIN Role_Permissions rp ON p.id = rp.permission_id
                    WHERE rp.role_id = @roleId
                    ORDER BY p.code ASC;";
                cmd.Parameters.AddWithValue("@roleId", roleId);

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

                Logger.Info($"Successfully retrieved {permissions.Count} permissions for role ID: {roleId}");
                return permissions;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permissions for role ID: {roleId}", ex);
                return null;
            }
        }

        #endregion
    }
}
