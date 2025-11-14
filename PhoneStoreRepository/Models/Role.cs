using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class Role
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;
        private string? _description;
        private int _weight = 100;
        private ICollection<RolePermission> _rolePermissions = new List<RolePermission>();
        private ICollection<AccountRole> _accountRoles = new List<AccountRole>();

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(50)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [MaxLength(255)]
        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        [Required]
        [Range(1, 1000)]
        public int Weight
        {
            get => _weight;
            set => _weight = value;
        }

        // Navigation properties
        public ICollection<RolePermission> RolePermissions
        {
            get => _rolePermissions;
            set => _rolePermissions = value ?? new List<RolePermission>();
        }

        public ICollection<AccountRole> AccountRoles
        {
            get => _accountRoles;
            set => _accountRoles = value ?? new List<AccountRole>();
        }

        // Constructors
        public Role()
        {
            _id = 0;
            _name = string.Empty;
            _description = null;
            _weight = 100;
            _rolePermissions = new List<RolePermission>();
            _accountRoles = new List<AccountRole>();
        }

        public Role(string name, int weight = 100)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _description = null;
            _weight = weight;
            _rolePermissions = new List<RolePermission>();
            _accountRoles = new List<AccountRole>();
        }
    }
}
