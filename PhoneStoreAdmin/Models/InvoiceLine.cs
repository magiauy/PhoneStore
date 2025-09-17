using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class InvoiceLine
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice { get; set; }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPct { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalPrice { get; set; }

        // Navigation property
        public ICollection<InvoiceLineSerial> InvoiceLineSerials { get; set; } = new List<InvoiceLineSerial>();
    }
}
