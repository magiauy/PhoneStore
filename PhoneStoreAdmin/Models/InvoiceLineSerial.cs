using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class InvoiceLineSerial
    {
        [Required]
        public int InvoiceLineId { get; set; }

        [Required]
        public int ProductSerialId { get; set; }
    }
}
