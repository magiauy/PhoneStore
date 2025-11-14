using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStore.Services.Interfaces
{
    public interface IRoleService
    {
        /// <summary>
        /// Get all roles
        /// </summary>
        /// <returns>List of all roles</returns>
        Task<List<Role>> GetAllRolesAsync();

        /// <summary>
        /// Get role by ID with permissions
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>Role if found, null otherwise</returns>
        Task<Role?> GetRoleByIdAsync(int roleId);

        /// <summary>
        /// Get role by name
        /// </summary>
        /// <param name="name">Role name</param>
        /// <returns>Role if found, null otherwise</returns>
        Task<Role?> GetRoleByNameAsync(string name);

        /// <summary>
        /// Create a new role
        /// </summary>
        /// <param name="role">Role to create</param>
        /// <returns>Created role with ID</returns>
        Task<Role> CreateRoleAsync(Role role);

        /// <summary>
        /// Update an existing role
        /// </summary>
        /// <param name="role">Role to update</param>
        /// <returns>True if update successful, false otherwise</returns>
        Task<bool> UpdateRoleAsync(Role role);

        /// <summary>
        /// Delete a role
        /// </summary>
        /// <param name="roleId">Role ID to delete</param>
        /// <returns>True if delete successful, false otherwise</returns>
        Task<bool> DeleteRoleAsync(int roleId);

        /// <summary>
        /// Update role permissions
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <param name="permissionIds">List of permission IDs to assign</param>
        /// <returns>True if update successful, false otherwise</returns>
        Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);

        /// <summary>
        /// Get permissions for a specific role
        /// </summary>
        /// <param name="roleId">Role ID</param>
        /// <returns>List of permissions</returns>
        Task<List<Permission>> GetRolePermissionsAsync(int roleId);
    }
}
