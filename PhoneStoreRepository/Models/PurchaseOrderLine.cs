using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
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
        private float _profitMargin = 0;

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

        /// <summary>
        /// Biên độ lợi nhuận (percentage, ví dụ: 0.20 = 20%)
        /// </summary>
        [Range(0, 1)]
        public float ProfitMargin
        {
            get => _profitMargin;
            set => _profitMargin = value;
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
            _profitMargin = 0;
        }

        public PurchaseOrderLine(int purchaseOrderId, int productId, int quantity, decimal unitCost, float profitMargin = 0)
        {
            _id = 0;
            _purchaseOrderId = purchaseOrderId;
            _productId = productId;
            _quantity = quantity;
            _unitCost = unitCost;
            _totalCost = quantity * unitCost;
            _profitMargin = profitMargin;
        }
    }
}
