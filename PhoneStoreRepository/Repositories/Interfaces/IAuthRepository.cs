using PhoneStoreRepository.Models;
using System.Threading.Tasks;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IAuthRepository
    {
        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Account if authentication successful, null otherwise</returns>
        Task<Account?> AuthenticateAsync(string username, string password);

        /// <summary>
        /// Validate user credentials
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if credentials are valid, false otherwise</returns>
        Task<bool> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// Get account by username
        /// </summary>
        /// <param name="username">Username</param>
        /// <returns>Account if found, null otherwise</returns>
        Task<Account?> GetAccountByUsernameAsync(string username);

        /// <summary>
        /// Update last login time for account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateLastLoginAsync(int accountId);

        /// <summary>
        /// Check if account is active and not locked
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if account is active, false otherwise</returns>
        Task<bool> IsAccountActiveAsync(int accountId);

        /// <summary>
        /// Get account with roles and permissions
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>Account with roles and permissions</returns>
        Task<Account?> GetAccountWithRolesAsync(int accountId);

        /// <summary>
        /// Create a new account
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <param name="personId">Person ID</param>
        /// <returns>Created account if successful, null otherwise</returns>
        Task<Account?> CreateAccountAsync(string username, string password, int personId);

        Task<Account?> ChangePasswordAsync(int accountId, string newPassword);
    }
}