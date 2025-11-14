using System;
using System.ComponentModel.DataAnnotations;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Models
{
    public class Payment
    {
        // Private backing fields
        private int _id;
        private int _invoiceId;
        private DateTime _paymentDate;
        private decimal _amount;
        private PaymentMethod _method;
        private string? _referenceNo;
        private string? _note;

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
        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => _paymentDate = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount
        {
            get => _amount;
            set => _amount = value;
        }

        [Required]
        public PaymentMethod Method
        {
            get => _method;
            set => _method = value;
        }

        [MaxLength(100)]
        public string? ReferenceNo
        {
            get => _referenceNo;
            set => _referenceNo = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Constructors
        public Payment()
        {
            _id = 0;
            _invoiceId = 0;
            _paymentDate = DateTime.UtcNow;
            _amount = 0;
            _method = PaymentMethod.CASH;
            _referenceNo = null;
            _note = null;
        }

        public Payment(int invoiceId, decimal amount, PaymentMethod method)
        {
            _id = 0;
            _invoiceId = invoiceId;
            _paymentDate = DateTime.UtcNow;
            _amount = amount;
            _method = method;
            _referenceNo = null;
            _note = null;
        }
    }
}
