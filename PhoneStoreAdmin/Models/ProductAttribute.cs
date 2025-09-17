using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class ProductAttribute
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(80)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public AttributeDataType DataType { get; set; } = AttributeDataType.text;

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
