using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        public async Task<List<Role>> GetAllRolesAsync()
        {
            try
            {
                Logger.Info("Getting all roles");
                var roles = await _roleRepository.GetAllAsync();
                var currentUserRole = UserSession.Instance.Role;
                
                if (currentUserRole != null)
                {
                    roles = roles?.Where(r => r.Weight > currentUserRole.Weight).ToList();
                }

                return roles ?? new List<Role>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all roles", ex);
                return new List<Role>();
            }
        }

        public async Task<Role?> GetRoleByIdAsync(int roleId)
        {
            try
            {
                Logger.Info($"Getting role by ID: {roleId}");
                return await _roleRepository.GetByIdAsync(roleId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get role by ID: {roleId}", ex);
                return null;
            }
        }

        public async Task<Role?> GetRoleByNameAsync(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return null;

                Logger.Info($"Getting role by name: {name}");
                return await _roleRepository.GetByNameAsync(name);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get role by name: {name}", ex);
                return null;
            }
        }

        public async Task<Role> CreateRoleAsync(Role role)
        {
            try
            {
                if (role == null)
                    throw new ArgumentNullException(nameof(role));

                // Validate weight against current user's role weight
                var currentUserRole = UserSession.Instance.Role;
                if (currentUserRole != null && role.Weight <= currentUserRole.Weight)
                {
                    Logger.Warning($"Cannot create role with weight {role.Weight} - must be greater than current user's role weight {currentUserRole.Weight}");
                    throw new InvalidOperationException($"Trọng số vai trò phải lớn hơn {currentUserRole.Weight} (trọng số vai trò của bạn)");
                }

                Logger.Info($"Creating new role: {role.Name}");
                var result = await _roleRepository.AddAsync(role);

                if (result != null)
                {
                    Logger.Info($"Successfully created role: {role.Name} (ID: {result.Id})");
                    return result;
                }

                Logger.Warning($"Failed to create role: {role.Name}");
                throw new InvalidOperationException($"Failed to create role: {role.Name}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create role: {role.Name}", ex);
                throw;
            }
        }

        public async Task<bool> UpdateRoleAsync(Role role)
        {
            try
            {
                if (role == null)
                    throw new ArgumentNullException(nameof(role));

                // Validate weight against current user's role weight
                var currentUserRole = UserSession.Instance.Role;
                if (currentUserRole != null && role.Weight <= currentUserRole.Weight)
                {
                    Logger.Warning($"Cannot update role with weight {role.Weight} - must be greater than current user's role weight {currentUserRole.Weight}");
                    throw new InvalidOperationException($"Trọng số vai trò phải lớn hơn {currentUserRole.Weight} (trọng số vai trò của bạn)");
                }

                Logger.Info($"Updating role: {role.Name} (ID: {role.Id})");
                await _roleRepository.UpdateAsync(role);

                Logger.Info($"Successfully updated role: {role.Name} (ID: {role.Id})");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update role: {role.Name} (ID: {role.Id})", ex);
                throw;
            }
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            try
            {
                Logger.Info($"Deleting role with ID: {roleId}");
                await _roleRepository.DeleteAsync(roleId);
                Logger.Info($"Successfully deleted role with ID: {roleId}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete role with ID: {roleId}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
        {
            try
            {
                Logger.Info($"Updating permissions for role ID: {roleId} ({permissionIds?.Count ?? 0} permissions)");
                return await _roleRepository.UpdateRolePermissionsAsync(roleId, permissionIds ?? new List<int>());
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update permissions for role ID: {roleId}", ex);
                return false;
            }
        }

        public async Task<List<Permission>> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                Logger.Info($"Getting permissions for role ID: {roleId}");
                var permissions = await _roleRepository.GetRolePermissionsAsync(roleId);
                return permissions ?? new List<Permission>();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permissions for role ID: {roleId}", ex);
                return new List<Permission>();
            }
        }
    }
}