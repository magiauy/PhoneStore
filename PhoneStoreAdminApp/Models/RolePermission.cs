using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class RolePermission
    {
        [Required]
        public int RoleId { get; set; }

        [Required]
        public int PermissionId { get; set; }
    }
}
