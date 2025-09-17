using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        [Required]
        public int SupplierId { get; set; }

        [Required]
        public int CreatedBy { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public PoStatus Status { get; set; } = PoStatus.draft;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; } = 0;

        [MaxLength(255)]
        public string? Note { get; set; }

        // Navigation property
        public ICollection<PurchaseOrderLine> PurchaseOrderLines { get; set; } = new List<PurchaseOrderLine>();
    }
}
