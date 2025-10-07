using System.ComponentModel.DataAnnotations;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    public class ProductSerial
    {
        // Private backing fields
        private int _id;
        private int _productId;
        private string? _serialNumber;
        private string? _imei1;
        private string? _imei2;
        private int _batchId;
        private SerialStatus _status = SerialStatus.IN_STOCK;
        private int? _purchaseOrderLineId;
        private string? _note;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int ProductId
        {
            get => _productId;
            set => _productId = value;
        }

        [MaxLength(100)]
        public string? SerialNumber
        {
            get => _serialNumber;
            set => _serialNumber = value;
        }

        [MaxLength(20)]
        public string? Imei1
        {
            get => _imei1;
            set => _imei1 = value;
        }

        [MaxLength(20)]
        public string? Imei2
        {
            get => _imei2;
            set => _imei2 = value;
        }

        [Required]
        public int BatchId
        {
            get => _batchId;
            set => _batchId = value;
        }

        [Required]
        public SerialStatus Status
        {
            get => _status;
            set => _status = value;
        }

        public int? PurchaseOrderLineId
        {
            get => _purchaseOrderLineId;
            set => _purchaseOrderLineId = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Constructors
        public ProductSerial()
        {
            _id = 0;
            _productId = 0;
            _serialNumber = null;
            _imei1 = null;
            _imei2 = null;
            _batchId = 0;
            _status = SerialStatus.IN_STOCK;
            _purchaseOrderLineId = null;
            _note = null;
        }

        public ProductSerial(int productId, int batchId)
        {
            _id = 0;
            _productId = productId;
            _serialNumber = null;
            _imei1 = null;
            _imei2 = null;
            _batchId = batchId;
            _status = SerialStatus.IN_STOCK;
            _purchaseOrderLineId = null;
            _note = null;
        }
    }
}
