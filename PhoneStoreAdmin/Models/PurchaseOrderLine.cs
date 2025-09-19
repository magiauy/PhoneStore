using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class PurchaseOrderLine
    {
        // Private backing fields
        private int _id;
        private int _purchaseOrderId;
        private int _productId;
        private int _quantity = 1;
        private decimal _unitCost = 0;
        private decimal _totalCost = 0;

        // Public properties with backing fields
        public int Id
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
        public decimal UnitCost
        {
            get => _unitCost;
            set => _unitCost = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal TotalCost
        {
            get => _totalCost;
            set => _totalCost = value;
        }

        // Constructors
        public PurchaseOrderLine()
        {
            _id = 0;
            _purchaseOrderId = 0;
            _productId = 0;
            _quantity = 1;
            _unitCost = 0;
            _totalCost = 0;
        }

        public PurchaseOrderLine(int purchaseOrderId, int productId, int quantity, decimal unitCost)
        {
            _id = 0;
            _purchaseOrderId = purchaseOrderId;
            _productId = productId;
            _quantity = quantity;
            _unitCost = unitCost;
            _totalCost = quantity * unitCost;
        }
    }
}
