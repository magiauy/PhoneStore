using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class ProductAttributeValue
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int AttributeId { get; set; }

        [MaxLength(255)]
        public string? ValueText { get; set; }

        public decimal? ValueNumber { get; set; }

        public DateTime? ValueDate { get; set; }

        public bool? ValueBool { get; set; }
    }
}
