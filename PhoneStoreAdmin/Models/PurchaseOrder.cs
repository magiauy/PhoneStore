using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class PurchaseOrder
    {
        // Private backing fields
        private int _id;
        private int _supplierId;
        private int _createdBy;
        private DateTime _orderDate;
        private PoStatus _status = PoStatus.DRAFT;
        private decimal _totalAmount = 0;
        private string? _note;
        private ICollection<PurchaseOrderLine> _purchaseOrderLines = new List<PurchaseOrderLine>();

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int SupplierId
        {
            get => _supplierId;
            set => _supplierId = value;
        }

        [Required]
        public int CreatedBy
        {
            get => _createdBy;
            set => _createdBy = value;
        }

        [Required]
        public DateTime OrderDate
        {
            get => _orderDate;
            set => _orderDate = value;
        }

        [Required]
        public PoStatus Status
        {
            get => _status;
            set => _status = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => _totalAmount = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Navigation property
        public ICollection<PurchaseOrderLine> PurchaseOrderLines
        {
            get => _purchaseOrderLines;
            set => _purchaseOrderLines = value ?? new List<PurchaseOrderLine>();
        }

        // Constructors
        public PurchaseOrder()
        {
            _id = 0;
            _supplierId = 0;
            _createdBy = 0;
            _orderDate = DateTime.Now;
            _status = PoStatus.DRAFT;
            _totalAmount = 0;
            _note = null;
            _purchaseOrderLines = new List<PurchaseOrderLine>();
        }

        public PurchaseOrder(int supplierId, int createdBy)
        {
            _id = 0;
            _supplierId = supplierId;
            _createdBy = createdBy;
            _orderDate = DateTime.Now;
            _status = PoStatus.DRAFT;
            _totalAmount = 0;
            _note = null;
            _purchaseOrderLines = new List<PurchaseOrderLine>();
        }
    }
}
