using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using PhoneStoreAdmin.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Models
{
    public class Invoice
    {
        // Private backing fields
        private int _id;
        private int? _personId;
        private int? _promotionCodeId;
        private int _createdBy;
        private DateTime _invoiceDate;
        private PhoneStoreAdmin.Models.Enums.InvoiceStatus _status;
        private decimal _totalAmount = 0;
        private decimal _discountAmount = 0;
        private decimal _finalAmount = 0;
        private PaymentMethod _paymentMethod = PaymentMethod.CASH;
        private string? _note;
        private ICollection<InvoiceLine> _invoiceLines = new List<InvoiceLine>();

        //Relationships
        public Person? Customer { get; set; }
        public PromotionCode? PromotionCode { get; set; }
        public Person? Creator { get; set; }

        public string CustomerName => Customer?.FullName ?? "Unknown";
        public string CreatedByName => Creator?.FullName ?? "System";
        public string DiscountCode => PromotionCode?.Code ?? "_";
        public enum InvoiceStatus { UNPAID,PAID,CANCELLED}
        
        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        public int? PersonId
        {
            get => _personId;
            set => _personId = value;
        }

        public int? PromotionCodeId
        {
            get => _promotionCodeId;
            set => _promotionCodeId = value;
        }

        [Required]
        public int CreatedBy
        {
            get => _createdBy;
            set => _createdBy = value;
        }

        [Required]
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set => _invoiceDate = value;
        }

        [Required]
        public PhoneStoreAdmin.Models.Enums.InvoiceStatus Status
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

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set => _discountAmount = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal FinalAmount
        {
            get => _finalAmount;
            set => _finalAmount = value;
        }

        [Required]
        public PaymentMethod PaymentMethod
        {
            get => _paymentMethod;
            set => _paymentMethod = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }
        // Navigation property
        public ICollection<InvoiceLine> InvoiceLines
        {
            get => _invoiceLines;
            set => _invoiceLines = value ?? new List<InvoiceLine>();
        }

        // Constructors
        public Invoice()
        {
            _id = 0;
            _personId = null;
            _promotionCodeId = null;
            _createdBy = 0;
            _invoiceDate = DateTime.UtcNow;
            _status = PhoneStoreAdmin.Models.Enums.InvoiceStatus.UNPAID;
            _totalAmount = 0;
            _discountAmount = 0;
            _finalAmount = 0;
            _paymentMethod = PaymentMethod.CASH;
            _note = null;
            _invoiceLines = new List<InvoiceLine>();
        }

        public Invoice(int createdBy, DateTime invoiceDate)
        {
            _id = 0;
            _personId = null;
            _promotionCodeId = null;
            _createdBy = createdBy;
            _invoiceDate = invoiceDate;
            _status = (Enums.InvoiceStatus)InvoiceStatus.UNPAID;
            _totalAmount = 0;
            _discountAmount = 0;
            _finalAmount = 0;
            _paymentMethod = PaymentMethod.CASH;
            _note = null;
            _invoiceLines = new List<InvoiceLine>();
        }
    }
}
