using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class ProductSerial
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [MaxLength(100)]
        public string? SerialNumber { get; set; }

        [MaxLength(20)]
        public string? Imei1 { get; set; }

        [MaxLength(20)]
        public string? Imei2 { get; set; }

        [Required]
        public int BatchProductId { get; set; }

        [Required]
        public SerialStatus Status { get; set; } = SerialStatus.in_stock;

        public int? PurchaseOrderLineId { get; set; }

        [MaxLength(255)]
        public string? Note { get; set; }
    }
}
