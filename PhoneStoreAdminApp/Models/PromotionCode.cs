using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class PromotionCode
    {
        public int Id { get; set; }

        [Required]
        public int PromotionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal MinimumAmount { get; set; }

        [Range(1, int.MaxValue)]
        public int? UsageLimit { get; set; }

        [Range(0, int.MaxValue)]
        public int UsedCount { get; set; } = 0;

        public bool IsActive { get; set; } = true;
    }
}
