using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class RolePermission
    {
        // Private backing fields
        private int _roleId;
        private int _permissionId;
        private Role _role = null!;
        private Permission _permission = null!;

        // Public properties with backing fields
        [Required]
        public int RoleId
        {
            get => _roleId;
            set => _roleId = value;
        }

        [Required]
        public int PermissionId
        {
            get => _permissionId;
            set => _permissionId = value;
        }

        // Navigation properties
        public Role Role
        {
            get => _role;
            set => _role = value;
        }

        public Permission Permission
        {
            get => _permission;
            set => _permission = value;
        }

        // Constructors
        public RolePermission()
        {
            _roleId = 0;
            _permissionId = 0;
            _role = null!;
            _permission = null!;
        }

        public RolePermission(int roleId, int permissionId)
        {
            _roleId = roleId;
            _permissionId = permissionId;
            _role = null!;
            _permission = null!;
        }
    }
}
