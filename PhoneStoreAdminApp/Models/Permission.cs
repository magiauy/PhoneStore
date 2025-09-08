using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class Permission
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }
    }
}
