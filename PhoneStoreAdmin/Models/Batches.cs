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
        // Private backing fields
        private int _id;
        private int _purchaseOrderId;
        private string? _batchCode;
        private DateTime _createdAt;
        private string? _note;

        // Public properties with backing fields
        [Required]
        public int id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int PurchaseOrderId
        {
            get => _purchaseOrderId;
            set => _purchaseOrderId = value;
        }

        [Required]
        public string? BatchCode
        {
            get => _batchCode;
            set => _batchCode = value;
        }

        [Required]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Constructors
        public Batches()
        {
            _id = 0;
            _purchaseOrderId = 0;
            _batchCode = null;
            _createdAt = DateTime.Now;
            _note = null;
        }

        public Batches(int purchaseOrderId, string? batchCode)
        {
            _id = 0;
            _purchaseOrderId = purchaseOrderId;
            _batchCode = batchCode;
            _createdAt = DateTime.Now;
            _note = null;
        }
    }
}
