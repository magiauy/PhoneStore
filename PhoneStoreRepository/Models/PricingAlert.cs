using System;
using System.ComponentModel.DataAnnotations;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Models
{
    /// <summary>
    /// Cảnh báo rủi ro tồn kho khi thị trường giảm giá
    /// </summary>
    public class PricingAlert
    {
        // Private backing fields
        private int _id;
        private int _productId;
        private decimal _variancePercent;
        private decimal _costFifoSnapshot;
        private decimal _costNifoSnapshot;
        private int _currentStock;
        private AlertStatus _status = AlertStatus.PENDING;
        private int? _resolvedBy;
        private DateTime? _resolvedAt;
        private string? _resolvedNote;
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
        /// Tỷ lệ chênh lệch giữa NIFO và FIFO (%)
        /// </summary>
        [Required]
        public decimal VariancePercent
        {
            get => _variancePercent;
            set => _variancePercent = value;
        }

        /// <summary>
        /// FIFO cost tại thời điểm tạo alert
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostFifoSnapshot
        {
            get => _costFifoSnapshot;
            set => _costFifoSnapshot = value;
        }

        /// <summary>
        /// NIFO cost tại thời điểm tạo alert
        /// </summary>
        [Required]
        [Range(0, double.MaxValue)]
        public decimal CostNifoSnapshot
        {
            get => _costNifoSnapshot;
            set => _costNifoSnapshot = value;
        }

        /// <summary>
        /// Số lượng tồn kho tại thời điểm tạo alert
        /// </summary>
        [Required]
        [Range(0, int.MaxValue)]
        public int CurrentStock
        {
            get => _currentStock;
            set => _currentStock = value;
        }

        /// <summary>
        /// Trạng thái của cảnh báo
        /// </summary>
        [Required]
        public AlertStatus Status
        {
            get => _status;
            set => _status = value;
        }

        /// <summary>
        /// ID của Admin đã xử lý cảnh báo
        /// </summary>
        public int? ResolvedBy
        {
            get => _resolvedBy;
            set => _resolvedBy = value;
        }

        /// <summary>
        /// Thời điểm xử lý cảnh báo
        /// </summary>
        public DateTime? ResolvedAt
        {
            get => _resolvedAt;
            set => _resolvedAt = value;
        }

        /// <summary>
        /// Ghi chú khi xử lý cảnh báo
        /// </summary>
        [MaxLength(500)]
        public string? ResolvedNote
        {
            get => _resolvedNote;
            set => _resolvedNote = value;
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
        public PricingAlert()
        {
            _id = 0;
            _productId = 0;
            _variancePercent = 0;
            _costFifoSnapshot = 0;
            _costNifoSnapshot = 0;
            _currentStock = 0;
            _status = AlertStatus.PENDING;
            _resolvedBy = null;
            _resolvedAt = null;
            _resolvedNote = null;
            _createdAt = DateTime.UtcNow;
            _product = null;
        }

        public PricingAlert(int productId, decimal variancePercent, decimal costFifo, decimal costNifo, int stock)
        {
            _id = 0;
            _productId = productId;
            _variancePercent = variancePercent;
            _costFifoSnapshot = costFifo;
            _costNifoSnapshot = costNifo;
            _currentStock = stock;
            _status = AlertStatus.PENDING;
            _resolvedBy = null;
            _resolvedAt = null;
            _resolvedNote = null;
            _createdAt = DateTime.UtcNow;
            _product = null;
        }
    }
}
