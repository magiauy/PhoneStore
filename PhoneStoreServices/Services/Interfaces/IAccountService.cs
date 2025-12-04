using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStore.Services.Interfaces
{
    public interface IAccountService
    {
        /// <summary>
        /// Get all accounts
        /// </summary>
        /// <returns>List of accounts</returns>
        //Task<IEnumerable<Account>> GetAllAsync();
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

        /// <summary>
        /// Get all accounts with person information
        /// </summary>
        /// <returns>List of all accounts</returns>
        Task<List<Account>?> GetAllAccountsAsync();

        /// <summary>
        /// Get paged accounts with person information
        /// </summary>
        /// <param name="pageIndex">Page index (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <param name="searchText">Optional search text for username or full name</param>
        /// <param name="filterCriteria">Optional filter criteria for advanced filtering</param>
        /// <returns>Tuple containing list of accounts and total count</returns>
        Task<(List<Account> Accounts, int TotalCount)?> GetPagedAccountsAsync(int pageIndex, int pageSize, string? searchText = null, AccountFilterCriteria? filterCriteria = null);

        /// <summary>
        /// Add a new account with person information
        /// </summary>
        /// <param name="account">Account to add (includes Employee or Customer)</param>
        /// <returns>ID of created account, or 0 if failed</returns>
        Task<int> AddAccountAsync(Account account);

        /// <summary>
        /// Update an existing account
        /// </summary>
        /// <param name="account">Account with updated fields</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        Task<bool> UpdateAccountAsync(Account account);

        /// <summary>
        /// Replace the list of role ids assigned to an account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <param name="roleIds">Collection of role IDs to assign</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        Task<bool> UpdateAccountRolesAsync(int accountId, IEnumerable<int> roleIds);

        /// <summary>
        /// Add account for an existing person (Employee or Customer) and assign roles
        /// </summary>
        /// <param name="username">Username for the new account</param>
        /// <param name="password">Plain text password (will be hashed)</param>
        /// <param name="personId">ID of existing person</param>
        /// <param name="roleIds">Collection of role IDs to assign</param>
        /// <param name="isActive">Whether account should be active</param>
        /// <returns>ID of created account, or 0 if failed</returns>
        Task<int> AddAccountForExistingPersonAsync(string username, string password, int personId, IEnumerable<int> roleIds, bool isActive = true);
    }
}
