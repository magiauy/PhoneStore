using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Supplier
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(160)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        [Phone]
        public string? Phone { get; set; }

        [MaxLength(120)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [Required]
        public bool IsActive { get; set; } = true;
    }
}
