using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class PromotionCode
    {
        // Private backing fields
        private int _id;
        private int _promotionId;
        private string _code = string.Empty;
        private decimal _discountAmount;
        private decimal _minimumAmount;
        private int? _usageLimit;
        private int _usedCount = 0;
        private bool _isActive = true;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int PromotionId
        {
            get => _promotionId;
            set => _promotionId = value;
        }

        [Required]
        [MaxLength(50)]
        public string Code
        {
            get => _code;
            set => _code = value ?? string.Empty;
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
        public decimal MinimumAmount
        {
            get => _minimumAmount;
            set => _minimumAmount = value;
        }

        [Range(1, int.MaxValue)]
        public int? UsageLimit
        {
            get => _usageLimit;
            set => _usageLimit = value;
        }

        [Range(0, int.MaxValue)]
        public int UsedCount
        {
            get => _usedCount;
            set => _usedCount = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Constructors
        public PromotionCode()
        {
            _id = 0;
            _promotionId = 0;
            _code = string.Empty;
            _discountAmount = 0;
            _minimumAmount = 0;
            _usageLimit = null;
            _usedCount = 0;
            _isActive = true;
        }

        public PromotionCode(int promotionId, string code, decimal discountAmount, decimal minimumAmount)
        {
            _id = 0;
            _promotionId = promotionId;
            _code = code ?? string.Empty;
            _discountAmount = discountAmount;
            _minimumAmount = minimumAmount;
            _usageLimit = null;
            _usedCount = 0;
            _isActive = true;
        }
    }
}
