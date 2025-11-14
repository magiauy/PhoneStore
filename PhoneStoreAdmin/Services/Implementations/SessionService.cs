using PhoneStoreRepository.Models;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class SessionService : ISessionService
    {
        private readonly IAuthService _authService;
        private readonly IAccountService _accountService;
        private readonly IPersonService _personService;
        private readonly UserSession _userSession;

        public SessionService(
            IAuthService authService,
            IAccountService accountService,
            IPersonService personService,
            UserSession userSession)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _personService = personService ?? throw new ArgumentNullException(nameof(personService));
            _userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                Logger.Info($"Session service attempting login for user: {username}");

                // Step 1: Authenticate user
                var account = await _authService.LoginAsync(username, password);
                if (account == null)
                {
                    Logger.Info($"Authentication failed for user: {username}");
                    return false;
                }

                Logger.Info($"Authentication successful for user: {username}, Account ID: {account.Id}");

                // Step 2: Get Person information
                var person = await _personService.GetPersonByIdAsync(account.PersonId);
                if (person == null)
                {
                    Logger.Warning($"Person not found for account ID: {account.PersonId}");
                }

                // Step 3: Get Roles
                var roles = await _accountService.GetRolesByAccountIdAsync(account.Id);
                var primaryRole = roles?.FirstOrDefault();
                if (primaryRole == null)
                {
                    Logger.Warning($"No roles found for account ID: {account.Id}");
                }

                // Step 4: Get Permissions
                var permissions = await _accountService.GetPermissionsByAccountIdAsync(account.Id);
                if (permissions == null || !permissions.Any())
                {
                    Logger.Warning($"No permissions found for account ID: {account.Id}");
                    permissions = new System.Collections.Generic.List<Permission>();
                }

                // Step 5: Initialize UserSession
                _userSession.Initialize(account, person, primaryRole, permissions);

                Logger.Info($"Session initialized successfully for user: {username}");
                Logger.Info($"User: {_userSession.GetDisplayName()}, Role: {_userSession.GetRoleName()}, Permissions: {_userSession.GetPermissionCount()}");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to login user: {username}", ex);
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                var currentUser = _userSession.GetDisplayName();
                Logger.Info($"Logging out user: {currentUser}");

                // Clear user session
                _userSession.Clear();

                // Call auth service logout for any additional cleanup
                await _authService.LogoutAsync();

                Logger.Info($"User logged out successfully: {currentUser}");
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to logout user", ex);
                // Still clear session even if other logout logic fails
                _userSession.Clear();
            }
        }

        public bool IsLoggedIn()
        {
            return _userSession.IsLoggedIn;
        }

        public UserSession GetCurrentSession()
        {
            return _userSession;
        }

        public bool HasPermission(string permissionCode)
        {
            return _userSession.HasPermission(permissionCode);
        }

        public string GetCurrentUserDisplayName()
        {
            return _userSession.GetDisplayName();
        }
    }
}