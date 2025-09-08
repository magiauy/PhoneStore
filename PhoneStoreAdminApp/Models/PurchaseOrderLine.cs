using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class PurchaseOrderLine
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitCost { get; set; } = 0;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalCost { get; set; } = 0;
    }
}
