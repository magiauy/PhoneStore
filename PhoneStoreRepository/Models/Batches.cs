using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhoneStoreRepository.Models
{
    public class Batches
    {
        // Private backing fields
        private int _id;
        private int _purchaseOrderId;
        private string? _batchCode;
        private DateTime _createdAt;
        private string? _note;
        private ICollection<BatchProduct> _batchProducts = new List<BatchProduct>();
        private PurchaseOrder _purchaseOrder = new PurchaseOrder();

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

        // Navigation property
        public ICollection<BatchProduct> BatchProducts
        {
            get => _batchProducts;
            set => _batchProducts = value ?? new List<BatchProduct>();
        }

        public PurchaseOrder PurchaseOrder
        {
            get => _purchaseOrder;
            set => _purchaseOrder = value ?? new PurchaseOrder();
        }

        // Constructors
        public Batches()
        {
            _id = 0;
            _purchaseOrderId = 0;
            _batchCode = null;
            _createdAt = DateTime.Now;
            _note = null;
            _batchProducts = new List<BatchProduct>();
        }

        public Batches(int purchaseOrderId, string? batchCode)
        {
            _id = 0;
            _purchaseOrderId = purchaseOrderId;
            _batchCode = batchCode;
            _createdAt = DateTime.Now;
            _note = null;
            _batchProducts = new List<BatchProduct>();
        }
    }
}