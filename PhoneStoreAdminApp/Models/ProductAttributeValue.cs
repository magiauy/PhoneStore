using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
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

        public DateOnly? ValueDate { get; set; }

        public bool? ValueBool { get; set; }
    }
}
