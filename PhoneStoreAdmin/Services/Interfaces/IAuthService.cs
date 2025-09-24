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
        /// Verify password for given username
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password to verify</param>
        /// <returns>True if password is correct</returns>
        Task<bool> VerifyPasswordAsync(string username, string password);

        /// <summary>
        /// Change password for user
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>Updated account if successful, null otherwise</returns>
        Task<Account?> ChangePasswordAsync(string username, string currentPassword, string newPassword);

        /// <summary>
        /// Logout current user (placeholder for future session management)
        /// </summary>
        Task LogoutAsync();
    }
}