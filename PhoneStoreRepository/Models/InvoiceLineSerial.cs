using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class InvoiceLineSerial
    {
        // Private backing fields
        private int _invoiceLineId;
        private int _productSerialId;

        // Public properties with backing fields
        [Required]
        public int InvoiceLineId
        {
            get => _invoiceLineId;
            set => _invoiceLineId = value;
        }

        [Required]
        public int ProductSerialId
        {
            get => _productSerialId;
            set => _productSerialId = value;
        }

        // Constructors
        public InvoiceLineSerial()
        {
            _invoiceLineId = 0;
            _productSerialId = 0;
        }

        public InvoiceLineSerial(int invoiceLineId, int productSerialId)
        {
            _invoiceLineId = invoiceLineId;
            _productSerialId = productSerialId;
        }
    }
}
