using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class ProductCategory
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(120)]
        public string Name { get; set; } = string.Empty;

        public int? ParentId { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
