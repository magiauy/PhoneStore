using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public async Task<List<Account>?> GetAllAccountsAsync()
        {
            try
            {
                Logger.Info("Getting all accounts");
                var accounts = await _accountRepository.GetAllAsync();
                if (accounts == null)
                    return null;

                var currentSession = UserSession.Instance;
                var currentAccountId = currentSession.Account?.Id;
                var currentRoleWeight = currentSession.Role?.Weight;

                var visibleAccounts = currentAccountId.HasValue
                    ? accounts.Where(a => a.Id != currentAccountId.Value).ToList()
                    : new List<Account>(accounts);

                if (!currentRoleWeight.HasValue)
                    return visibleAccounts;

                var filteredAccounts = new List<Account>();

                foreach (var account in visibleAccounts)
                {
                    var roles = await _accountRepository.GetRolesByAccountIdAsync(account.Id);

                    if (roles == null || roles.Count == 0)
                    {
                        filteredAccounts.Add(account);
                        continue;
                    }

                    var minRoleWeight = roles.Min(role => role.Weight);

                    if (minRoleWeight > currentRoleWeight.Value)
                    {
                        filteredAccounts.Add(account);
                    }
                }

                return filteredAccounts;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all accounts", ex);
                return null;
            }
        }

        public async Task<(List<Account> Accounts, int TotalCount)?> GetPagedAccountsAsync(int pageIndex, int pageSize, string? searchText = null, AccountFilterCriteria? filterCriteria = null)
        {
            try
            {
                var searchInfo = string.IsNullOrWhiteSpace(searchText) ? "all" : $"search: '{searchText}'";
                var filterInfo = filterCriteria?.HasAnyFilter() == true ? $", filters: {filterCriteria.GetCacheKey()}" : "";
                Logger.Info($"Getting paged accounts (page: {pageIndex}, size: {pageSize}, {searchInfo}{filterInfo})");
                return await _accountRepository.GetPagedAsync(pageIndex, pageSize, searchText, filterCriteria);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get paged accounts (page: {pageIndex}, size: {pageSize})", ex);
                return null;
            }
        }

        public async Task<int> AddAccountAsync(Account account)
        {
            try
            {
                Logger.Info($"Adding new account: {account.Username}");
                var result = await _accountRepository.AddAsync(account);
                
                if (result != null && result.Id > 0)
                {
                    Logger.Info($"Successfully added account: {account.Username} (ID: {result.Id})");
                    return result.Id;
                }
                else
                {
                    Logger.Warning($"Failed to add account: {account.Username} - no account returned");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to add account: {account.Username}", ex);
                return 0;
            }
        }

        public async Task<bool> UpdateAccountAsync(Account account)
        {
            try
            {
                if (account == null)
                    throw new ArgumentNullException(nameof(account));

                Logger.Info($"Updating account ID: {account.Id}");
                await _accountRepository.UpdateAsync(account);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update account ID: {account?.Id}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateAccountRolesAsync(int accountId, IEnumerable<int> roleIds)
        {
            try
            {
                var normalizedRoleIds = roleIds?.Distinct().ToList() ?? new List<int>();
                Logger.Info($"Updating roles for account ID: {accountId} (count: {normalizedRoleIds.Count})");
                await _accountRepository.UpdateRolesAsync(accountId, normalizedRoleIds);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update roles for account ID: {accountId}", ex);
                return false;
            }
        }
    }
}
