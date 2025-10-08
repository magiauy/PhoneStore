using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IAccountRepository : IRepository<Account>
    {
        Account GetByUsername(string username);

        /// <summary>
        /// Get account by username async
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account?> GetByUsernameAsync(string username);

        /// <summary>
        /// Get account with full details including roles and permissions
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account with roles and permissions</returns>
        Task<Account?> GetWithRolesAsync(int accountId);

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
        Task<bool> IsActiveAsync(int accountId);

        /// <summary>
        /// Update last login time for account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateLastLoginAsync(int accountId);

        /// <summary>
        /// Get account by ID async
        /// </summary>
        /// <param name="id">Account ID</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account?> GetByIdAsync(int id);

        /// <summary>
        /// Add account async
        /// </summary>
        /// <param name="entity">Account to add</param>
        /// <returns>Added account if successful, null otherwise</returns>
        Task<Account?> AddAsync(Account entity);

        /// <summary>
        /// Update account async
        /// </summary>
        /// <param name="entity">Account to update</param>
        Task UpdateAsync(Account entity);

        /// <summary>
        /// Delete account async
        /// </summary>
        /// <param name="id">Account ID to delete</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// Get all accounts with person information
        /// </summary>
        /// <returns>List of all accounts</returns>
        Task<List<Account>?> GetAllAsync();

        /// <summary>
        /// Get paged accounts with person information
        /// </summary>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="searchText">Optional search text for username or full name</param>
        /// <param name="filterCriteria">Optional filter criteria for advanced filtering</param>
        /// <returns>Tuple containing list of accounts and total count</returns>
        Task<(List<Account> Accounts, int TotalCount)?> GetPagedAsync(int pageIndex, int pageSize, string? searchText = null, AccountFilterCriteria? filterCriteria = null);
    }
}
