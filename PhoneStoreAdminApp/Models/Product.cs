using System.ComponentModel.DataAnnotations;
using PhoneStoreAdminApp.Models.Enums;

namespace PhoneStoreAdminApp.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int CategoryId { get; set; }

        public int? BrandId { get; set; }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Cost { get; set; } = 0;

        [Required]
        public bool IsSerialTracked { get; set; } = false;

        [Required]
        [Range(0, 240)]
        public int WarrantyMonths { get; set; } = 12;

        [Required]
        public ProductStatus Status { get; set; } = ProductStatus.active;

        [Required]
        public DateTime CreatedAt { get; set; }
        // Navigation: product has many attribute values
        public ICollection<ProductAttributeValue> ProductAttributeValues { get; set; } = new List<ProductAttributeValue>();
    }
}
