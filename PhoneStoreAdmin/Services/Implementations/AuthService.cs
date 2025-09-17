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
        private Account? _currentUser;

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

                    // Store current user for session management
                    _currentUser = account;
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

        public Task LogoutAsync()
        {
            // Clear current user session
            _currentUser = null;
            
            // In future, this could handle additional logout logic like:
            // - Clearing cached data
            // - Invalidating tokens
            // - Logging logout event
            
            return Task.CompletedTask;
        }

        public Account? GetCurrentUser()
        {
            return _currentUser;
        }
    }
}