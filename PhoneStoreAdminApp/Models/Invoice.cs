using System.ComponentModel.DataAnnotations;
using PhoneStoreAdminApp.Models.Enums;

namespace PhoneStoreAdminApp.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        public int? PersonId { get; set; }

        public int? PromotionCodeId { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime InvoiceDate { get; set; }

        [Required]
        public InvoiceStatus Status { get; set; } = InvoiceStatus.unpaid;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FinalAmount { get; set; } = 0;

        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.cash;

        [MaxLength(255)]
        public string? Note { get; set; }

        // Navigation property
        public ICollection<InvoiceLine> InvoiceLines { get; set; } = new List<InvoiceLine>();
    }
}
