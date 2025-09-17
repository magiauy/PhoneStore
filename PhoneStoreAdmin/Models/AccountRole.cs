using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class AccountRole
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        public int RoleId { get; set; }

        // Navigation properties
        public Account Account { get; set; } = null!;
        public Role Role { get; set; } = null!;
    }
}
