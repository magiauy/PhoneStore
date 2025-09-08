using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class Promotion
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation property
        public ICollection<PromotionCode> PromotionCodes { get; set; } = new List<PromotionCode>();
    }
}
