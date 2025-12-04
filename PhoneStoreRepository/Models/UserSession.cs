using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreRepository.Models
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
        public List<Role>? Roles { get; private set; }
        public List<Permission>? Permissions { get; private set; }

        public bool IsLoggedIn => Account != null;

        public void Initialize(Account account, Person? person, Role? role, List<Permission>? permissions)
        {
            Account = account;
            Person = person;
            Role = role;
            Roles = role != null ? new List<Role> { role } : new List<Role>();
            Permissions = permissions ?? new List<Permission>();
        }

        public void Initialize(Account account, Person? person, List<Role>? roles, List<Permission>? permissions)
        {
            Account = account;
            Person = person;
            Roles = roles ?? new List<Role>();
            Role = Roles.OrderBy(r => r.Weight).FirstOrDefault(); // Role with lowest weight (highest privilege)
            Permissions = permissions ?? new List<Permission>();
        }

        /// <summary>
        /// Get the minimum (highest privilege) weight among all roles of the current user
        /// Lower weight = higher privilege (e.g., Admin=0, Manager=50, Staff=100)
        /// </summary>
        /// <returns>Minimum weight, or int.MaxValue if no roles</returns>
        public int GetMaxWeight()
        {
            if (Roles == null || Roles.Count == 0)
            {
                return Role?.Weight ?? int.MaxValue;
            }
            return Roles.Min(r => r.Weight);
        }

        public bool HasPermission(string permissionCode)
        {
            if (Permissions == null || string.IsNullOrWhiteSpace(permissionCode))
                return false;

            return Permissions.Any(p => p.Code?.Equals(permissionCode, System.StringComparison.OrdinalIgnoreCase) == true) || GetMaxWeight() == 0;
        }

        public bool HasAnyPermission(params string[] permissionCodes)
        {
            if (Permissions == null || permissionCodes == null || permissionCodes.Length == 0)
                return false;

            return permissionCodes.Any(HasPermission) || GetMaxWeight() == 0;
        }

        public bool HasAllPermissions(params string[] permissionCodes)
        {
            if (Permissions == null || permissionCodes == null || permissionCodes.Length == 0)
                return false;

            return permissionCodes.All(HasPermission) || GetMaxWeight() == 0;
        }

        public void Clear()
        {
            Account = null;
            Person = null;
            Role = null;
            Roles = null;
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

        //UpdatePersonInfo
        public void UpdatePersonInfo(Person updatedPerson)
        {
            if (Person != null && updatedPerson != null && Person.Id == updatedPerson.Id)
            {
                Person = updatedPerson;
            }
        }
    }
}