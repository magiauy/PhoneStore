using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;

        public AccountService(IAccountRepository accountRepository)
        {
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        }

        public async Task<Account?> GetAccountByIdAsync(int accountId)
        {
            try
            {
                Logger.Info($"Getting account by ID: {accountId}");
                return await _accountRepository.GetByIdAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account by ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<Account?> GetAccountByUsernameAsync(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return null;

                Logger.Info($"Getting account by username: {username}");
                return await _accountRepository.GetByUsernameAsync(username);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account by username: {username}", ex);
                return null;
            }
        }

        public async Task<Account?> GetAccountWithRolesAsync(int accountId)
        {
            try
            {
                Logger.Info($"Getting account with roles by ID: {accountId}");
                return await _accountRepository.GetWithRolesAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get account with roles by ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<List<Role>?> GetRolesByAccountIdAsync(int accountId)
        {
            try
            {
                Logger.Info($"Getting roles for account ID: {accountId}");
                return await _accountRepository.GetRolesByAccountIdAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get roles for account ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<List<Permission>?> GetPermissionsByAccountIdAsync(int accountId)
        {
            try
            {
                Logger.Info($"Getting permissions for account ID: {accountId}");
                return await _accountRepository.GetPermissionsByAccountIdAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get permissions for account ID: {accountId}", ex);
                return null;
            }
        }

        public async Task<bool> IsAccountActiveAsync(int accountId)
        {
            try
            {
                return await _accountRepository.IsActiveAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check if account is active for ID: {accountId}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateLastLoginAsync(int accountId)
        {
            try
            {
                Logger.Info($"Updating last login for account ID: {accountId}");
                return await _accountRepository.UpdateLastLoginAsync(accountId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update last login for account ID: {accountId}", ex);
                return false;
            }
        }
    }
}