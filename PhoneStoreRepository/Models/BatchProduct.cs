using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStoreRepository.Models
{
    public class BatchProduct
    {
        // Private backing fields
        private int _id;
        private int _batchId;
        private int _productId;
        private int _quantity = 0;
        private decimal _costPrice = 0;
        private decimal _sellingPrice = 0;
        private decimal _profitMargin = 0;

        // Public properties with backing fields
        [Key]
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int BatchId
        {
            get => _batchId;
            set => _batchId = value;
        }

        [Required]
        public int ProductId
        {
            get => _productId;
            set => _productId = value;
        }

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity
        {
            get => _quantity;
            set => _quantity = value;
        }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal CostPrice
        {
            get => _costPrice;
            set => _costPrice = value;
        }

        [Required]
        [Column(TypeName = "decimal(12,2)")]
        public decimal SellingPrice
        {
            get => _sellingPrice;
            set => _sellingPrice = value;
        }

        /// <summary>
        /// Biên độ lợi nhuận (percentage, ví dụ: 0.20 = 20%)
        /// </summary>
        [Range(0, 1)]
        public decimal ProfitMargin
        {
            get => _profitMargin;
            set => _profitMargin = value;
        }

        // Constructors
        public BatchProduct()
        {
            _id = 0;
            _batchId = 0;
            _productId = 0;
            _quantity = 0;
            _costPrice = 0;
            _sellingPrice = 0;
            _profitMargin = 0;
        }

        public BatchProduct(int batchId, int productId, int quantity, decimal costPrice, decimal sellingPrice, decimal profitMargin = 0)
        {
            _id = 0;
            _batchId = batchId;
            _productId = productId;
            _quantity = quantity;
            _costPrice = costPrice;
            _sellingPrice = sellingPrice;
            _profitMargin = profitMargin;
        }
    }
}