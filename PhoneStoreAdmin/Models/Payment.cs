using System;
using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class Payment
    {
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [Required]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public PaymentMethod Method { get; set; }

        [MaxLength(100)]
        public string? ReferenceNo { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
