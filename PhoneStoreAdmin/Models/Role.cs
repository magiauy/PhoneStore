using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Role
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [Range(1, 1000)]
        public int Weight { get; set; } = 100;

        // Navigation properties
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
    }
}
