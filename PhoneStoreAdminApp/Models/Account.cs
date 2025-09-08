using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class Account
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public int PersonId { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;

        [Required]
        public DateTime CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
    }
}
