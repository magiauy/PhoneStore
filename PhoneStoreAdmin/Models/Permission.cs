using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Permission
    {
        // Private backing fields
        private int _id;
        private string _code = string.Empty;
        private string? _description;
        private ICollection<RolePermission> _rolePermissions = new List<RolePermission>();

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(100)]
        public string Code
        {
            get => _code;
            set => _code = value ?? string.Empty;
        }

        [MaxLength(255)]
        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        // Navigation property
        public ICollection<RolePermission> RolePermissions
        {
            get => _rolePermissions;
            set => _rolePermissions = value ?? new List<RolePermission>();
        }

        // Constructors
        public Permission()
        {
            _id = 0;
            _code = string.Empty;
            _description = null;
            _rolePermissions = new List<RolePermission>();
        }

        public Permission(string code, string? description = null)
        {
            _id = 0;
            _code = code ?? string.Empty;
            _description = description;
            _rolePermissions = new List<RolePermission>();
        }
    }
}
