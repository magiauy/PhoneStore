using PhoneStoreAdmin.Models;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface ISessionService
    {
        /// <summary>
        /// Login user and initialize session
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <returns>True if login successful and session initialized</returns>
        Task<bool> LoginAsync(string username, string password);

        /// <summary>
        /// Logout user and clear session
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// Check if user is currently logged in
        /// </summary>
        /// <returns>True if user is logged in</returns>
        bool IsLoggedIn();

        /// <summary>
        /// Get current user session
        /// </summary>
        /// <returns>UserSession instance</returns>
        UserSession GetCurrentSession();

        /// <summary>
        /// Check if current user has specific permission
        /// </summary>
        /// <param name="permissionCode">Permission code</param>
        /// <returns>True if user has permission</returns>
        bool HasPermission(string permissionCode);

        /// <summary>
        /// Get current user's display name
        /// </summary>
        /// <returns>Display name or "Unknown User"</returns>
        string GetCurrentUserDisplayName();
    }
}