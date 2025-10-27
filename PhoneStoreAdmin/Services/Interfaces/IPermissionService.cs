using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IPermissionService
    {
        /// <summary>
        /// Get all permissions
        /// </summary>
        /// <returns>List of all permissions</returns>
        Task<List<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// Get permission by ID
        /// </summary>
        /// <param name="permissionId">Permission ID</param>
        /// <returns>Permission if found, null otherwise</returns>
        Task<Permission?> GetPermissionByIdAsync(int permissionId);

        /// <summary>
        /// Get permission by code
        /// </summary>
        /// <param name="code">Permission code</param>
        /// <returns>Permission if found, null otherwise</returns>
        Task<Permission?> GetPermissionByCodeAsync(string code);

        /// <summary>
        /// Create a new permission
        /// </summary>
        /// <param name="permission">Permission to create</param>
        /// <returns>Created permission with ID</returns>
        Task<Permission> CreatePermissionAsync(Permission permission);

        /// <summary>
        /// Update an existing permission
        /// </summary>
        /// <param name="permission">Permission to update</param>
        /// <returns>True if update successful, false otherwise</returns>
        Task<bool> UpdatePermissionAsync(Permission permission);

        /// <summary>
        /// Delete a permission
        /// </summary>
        /// <param name="permissionId">Permission ID to delete</param>
        /// <returns>True if delete successful, false otherwise</returns>
        Task<bool> DeletePermissionAsync(int permissionId);
    }
}
