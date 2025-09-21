using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IAccountService
    {
        /// <summary>
        /// Get account by ID
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account?> GetAccountByIdAsync(int accountId);

        /// <summary>
        /// Get account by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account?> GetAccountByUsernameAsync(string username);

        /// <summary>
        /// Get account with full details including roles and permissions
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account with roles and permissions</returns>
        Task<Account?> GetAccountWithRolesAsync(int accountId);

        /// <summary>
        /// Get roles associated with account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>List of roles</returns>
        Task<List<Role>?> GetRolesByAccountIdAsync(int accountId);

        /// <summary>
        /// Get permissions associated with account through roles
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>List of permissions</returns>
        Task<List<Permission>?> GetPermissionsByAccountIdAsync(int accountId);

        /// <summary>
        /// Check if account is active and not locked
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if account is active, false otherwise</returns>
        Task<bool> IsAccountActiveAsync(int accountId);

        /// <summary>
        /// Update last login time for account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateLastLoginAsync(int accountId);
    }
}