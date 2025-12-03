using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    /// <summary>
    /// Lịch sử biến động giá sản phẩm
    /// </summary>
    public class PricingHistory
    {
        // Private backing fields
        private int _id;
        private int _productId;
        private decimal _oldPrice;
        private decimal _newPrice;
        private decimal _costFifo;
        private decimal _costNifo;
        private string _changeReason = string.Empty;
        private int? _changedBy;
        private DateTime _createdAt;

        // Navigation
        private Product? _product;

        // Public properties

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

        /// <summary>
        /// Giá cũ trước khi thay đổi
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal OldPrice
        {
            get => _oldPrice;
            set => _oldPrice = value;
        }

        /// <summary>
        /// Giá mới sau khi thay đổi
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal NewPrice
        {
            get => _newPrice;
            set => _newPrice = value;
        }

        /// <summary>
        /// FIFO cost tại thời điểm thay đổi giá
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostFifo
        {
            get => _costFifo;
            set => _costFifo = value;
        }

        /// <summary>
        /// NIFO cost tại thời điểm thay đổi giá
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostNifo
        {
            get => _costNifo;
            set => _costNifo = value;
        }

        /// <summary>
        /// Lý do thay đổi giá: AUTO_INCREASE, CLEARANCE, MANUAL
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string ChangeReason
        {
            get => _changeReason;
            set => _changeReason = value ?? string.Empty;
        }

        /// <summary>
        /// ID của người thay đổi (null = hệ thống tự động)
        /// </summary>
        public int? ChangedBy
        {
            get => _changedBy;
            set => _changedBy = value;
        }

        [Required]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        // Navigation property
        public Product? Product
        {
            get => _product;
            set => _product = value;
        }

        // Constructors
        public PricingHistory()
        {
            _id = 0;
            _productId = 0;
            _oldPrice = 0;
            _newPrice = 0;
            _costFifo = 0;
            _costNifo = 0;
            _changeReason = string.Empty;
            _changedBy = null;
            _createdAt = DateTime.UtcNow;
            _product = null;
        }

        public PricingHistory(int productId, decimal oldPrice, decimal newPrice, decimal costFifo, decimal costNifo, string changeReason)
        {
            _id = 0;
            _productId = productId;
            _oldPrice = oldPrice;
            _newPrice = newPrice;
            _costFifo = costFifo;
            _costNifo = costNifo;
            _changeReason = changeReason ?? string.Empty;
            _changedBy = null;
            _createdAt = DateTime.UtcNow;
            _product = null;
        }
    }
}
