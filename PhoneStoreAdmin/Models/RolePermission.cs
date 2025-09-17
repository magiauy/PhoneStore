using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class RolePermission
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int PermissionId { get; set; }

        // Navigation properties
        public Role Role { get; set; } = null!;
        public Permission Permission { get; set; } = null!;
    }
}
