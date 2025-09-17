using PhoneStoreAdmin.Models;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate user with username and password
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>Account if authentication successful, null otherwise</returns>
        Task<Account?> LoginAsync(string username, string password);

        /// <summary>
        /// Validate user credentials without returning account data
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if credentials are valid</returns>
        Task<bool> ValidateCredentialsAsync(string username, string password);

        /// <summary>
        /// Create new account
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Plain text password</param>
        /// <param name="personId">Person ID</param>
        /// <returns>Created account if successful, null otherwise</returns>
        Task<Account?> CreateAccountAsync(string username, string password, int personId);

        /// <summary>
        /// Update last login time for account
        /// </summary>
        /// <param name="accountId">Account ID</param>
        /// <returns>True if updated successfully</returns>
        Task<bool> UpdateLastLoginAsync(int accountId);

        /// <summary>
        /// Logout current user (placeholder for future session management)
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Get current authenticated user (placeholder for future session management)
        /// </summary>
        /// <returns>Current account or null if not authenticated</returns>
        Account? GetCurrentUser();
    }
}