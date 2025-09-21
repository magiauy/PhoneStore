using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.Models
{
    public class UserSession
    {
        private static UserSession? _instance;
        private static readonly object _lock = new object();

        public static UserSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new UserSession();
                        }
                    }
                }
                return _instance;
            }
        }

        private UserSession() { }

        public Account? Account { get; private set; }
        public Person? Person { get; private set; }
        public Role? Role { get; private set; }
        public List<Permission>? Permissions { get; private set; }

        public bool IsLoggedIn => Account != null;

        public void Initialize(Account account, Person? person, Role? role, List<Permission>? permissions)
        {
            Account = account;
            Person = person;
            Role = role;
            Permissions = permissions ?? new List<Permission>();
        }

        public bool HasPermission(string permissionCode)
        {
            if (Permissions == null || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return Permissions.Any(p => p.Code?.Equals(permissionCode, System.StringComparison.OrdinalIgnoreCase) == true);
        }

        public bool HasAnyPermission(params string[] permissionCodes)
        {
            if (Permissions == null || permissionCodes == null || permissionCodes.Length == 0)
                return false;

            return permissionCodes.Any(HasPermission);
        }

        public bool HasAllPermissions(params string[] permissionCodes)
        {
            if (Permissions == null || permissionCodes == null || permissionCodes.Length == 0)
                return false;

            return permissionCodes.All(HasPermission);
        }

        public void Clear()
        {
            Account = null;
            Person = null;
            Role = null;
            Permissions = null;
        }

        public string GetDisplayName()
        {
            if (Person != null)
            {
                return Person.FullName;
            }
            return Account?.Username ?? "Unknown User";
        }

        public string GetRoleName()
        {
            return Role?.Name ?? "No Role";
        }

        public int GetPermissionCount()
        {
            return Permissions?.Count ?? 0;
        }
    }
}