using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Models
{
    public class Batches
    {
        [Required]
        public int id { get; set; }
        [Required]
        public int PurchaseOrderId { get; set; }
        [Required]
        public string? BatchCode { get; set; }
        [Required]
        public DateTime CreatedAt { get; set; }
        [MaxLength(255)]
        public string? Note { get; set; }

    }
}
