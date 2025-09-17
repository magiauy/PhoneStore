using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStoreAdmin.Models
{
    public class BatchProduct
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BatchId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal CostPrice { get; set; } = 0;

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal SellingPrice { get; set; } = 0;
    }
}