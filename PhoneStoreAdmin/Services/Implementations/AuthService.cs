using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository _authRepository;

        public AuthService(IAuthRepository authRepository)
        {
            _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        }

        public async Task<Account?> LoginAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return null;
                }
                Logger.Info($"Service Authenticating user: {username}");

                // Validate credentials and get account
                var account = await _authRepository.AuthenticateAsync(username, password);
                Logger.Info($"Service Authentication result for user {username}: {(account != null ? "Success" : "Failed")}");
                if (account != null)
                {
                    // Update last login time
                    await _authRepository.UpdateLastLoginAsync(account.Id);
                }

                return account;
            }
            catch (Exception ex)
            {
                // Log error if needed (for now just return null)
                Logger.Error($"Service Failed to authenticate user: {username}", ex);
                return null;
            }
        }

        public async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            try
            {
                return await _authRepository.ValidateCredentialsAsync(username, password);
            }
            catch (Exception ex)
            {
                Logger.Error($"Service Failed to validate credentials for user: {username}", ex);
                return false;
            }
        }

        public async Task<Account?> CreateAccountAsync(string username, string password, int personId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return null;
                }
                Logger.Info($"Service Creating account for user: {username}");

                return await _authRepository.CreateAccountAsync(username, password, personId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Service Failed to create account for user: {username}", ex);
                return null;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int accountId)
        {
            try
            {
                return await _authRepository.UpdateLastLoginAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Service Failed to update last login for account ID: {accountId}", ex);
                return false;
            }
        }

        public async Task<bool> VerifyPasswordAsync(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    return false;
                }
                Logger.Info($"Service Verifying password for user: {username}");

                return await _authRepository.ValidateCredentialsAsync(username, password);
            }
            catch (Exception ex)
            {
                Logger.Error($"Service Failed to verify password for user: {username}", ex);
                return false;
            }
        }

        public async Task<Account?> ChangePasswordAsync(string username, string currentPassword, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || 
                    string.IsNullOrWhiteSpace(currentPassword) || 
                    string.IsNullOrWhiteSpace(newPassword))
                {
                    return null;
                }
                Logger.Info($"Service Changing password for user: {username}");

                // First verify current password
                var isCurrentPasswordValid = await _authRepository.ValidateCredentialsAsync(username, currentPassword);
                if (!isCurrentPasswordValid)
                {
                    Logger.Warning($"Service Current password verification failed for user: {username}");
                    return null;
                }

                // Get account to get ID
                var account = await _authRepository.AuthenticateAsync(username, currentPassword);
                if (account == null)
                {
                    Logger.Warning($"Service Could not retrieve account for user: {username}");
                    return null;
                }

                // Change password using account ID
                return await _authRepository.ChangePasswordAsync(account.Id, newPassword);
            }
            catch (Exception ex)
            {
                Logger.Error($"Service Failed to change password for user: {username}", ex);
                return null;
            }
        }

        public Task LogoutAsync()
        {
            // AuthService no longer manages user session
            // Session management is now handled by UserSession singleton
            
            // In future, this could handle additional logout logic like:
            // - Clearing cached data
            // - Invalidating tokens
            // - Logging logout event
            
            return Task.CompletedTask;
        }
    }
}