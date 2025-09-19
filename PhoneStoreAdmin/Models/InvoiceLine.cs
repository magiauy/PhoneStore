using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class InvoiceLine
    {
        // Private backing fields
        private int _id;
        private int _invoiceId;
        private int _productId;
        private int _quantity = 1;
        private decimal _unitPrice;
        private decimal _discountPct = 0;
        private decimal _totalPrice;
        private ICollection<InvoiceLineSerial> _invoiceLineSerials = new List<InvoiceLineSerial>();

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int InvoiceId
        {
            get => _invoiceId;
            set => _invoiceId = value;
        }

        [Required]
        public int ProductId
        {
            get => _productId;
            set => _productId = value;
        }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity
        {
            get => _quantity;
            set => _quantity = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal UnitPrice
        {
            get => _unitPrice;
            set => _unitPrice = value;
        }

        [Required]
        [Range(0, 100)]
        public decimal DiscountPct
        {
            get => _discountPct;
            set => _discountPct = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalPrice
        {
            get => _totalPrice;
            set => _totalPrice = value;
        }

        // Navigation property
        public ICollection<InvoiceLineSerial> InvoiceLineSerials
        {
            get => _invoiceLineSerials;
            set => _invoiceLineSerials = value ?? new List<InvoiceLineSerial>();
        }

        // Constructors
        public InvoiceLine()
        {
            _id = 0;
            _invoiceId = 0;
            _productId = 0;
            _quantity = 1;
            _unitPrice = 0;
            _discountPct = 0;
            _totalPrice = 0;
            _invoiceLineSerials = new List<InvoiceLineSerial>();
        }

        public InvoiceLine(int invoiceId, int productId, int quantity, decimal unitPrice)
        {
            _id = 0;
            _invoiceId = invoiceId;
            _productId = productId;
            _quantity = quantity;
            _unitPrice = unitPrice;
            _discountPct = 0;
            _totalPrice = quantity * unitPrice;
            _invoiceLineSerials = new List<InvoiceLineSerial>();
        }
    }
}
