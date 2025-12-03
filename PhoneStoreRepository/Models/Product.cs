using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Models
{
    public class Product
    {
        // Private backing fields
        private int _id;
        private string _sku = string.Empty;
        private string _name = string.Empty;
        private int _categoryId;
        private int _modelId;
        private int? _brandId;
        private decimal _price = 0;
        private decimal _cost = 0;
        private bool _isSerialTracked = false;
        private int _warrantyMonths = 12;
        private ProductStatus _status = ProductStatus.ACTIVE;
        private DateTime _createdAt;
        private ICollection<ProductAttributeValue> _productAttributeValues = new List<ProductAttributeValue>();

        // Dynamic Pricing fields
        private decimal _costFifo = 0;
        private decimal _costNifo = 0;
        private MarketTrend _marketTrend = MarketTrend.STABLE;
        private PricingMode _pricingMode = PricingMode.AUTO_PROTECT;
        private DateTime? _priceUpdatedAt;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(50)]
        public string Sku
        {
            get => _sku;
            set => _sku = value ?? string.Empty;
        }

        [Required]
        [MaxLength(200)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [Required]
        public int CategoryId
        {
            get => _categoryId;
            set => _categoryId = value;
        }

        [Required]
        public int ModelId
        {
            get => _modelId;
            set => _modelId = value;
        }

        public int? BrandId
        {
            get => _brandId;
            set => _brandId = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price
        {
            get => _price;
            set => _price = value;
        }

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Cost
        {
            get => _cost;
            set => _cost = value;
        }

        [Required]
        public bool IsSerialTracked
        {
            get => _isSerialTracked;
            set => _isSerialTracked = value;
        }

        [Required]
        [Range(0, 240)]
        public int WarrantyMonths
        {
            get => _warrantyMonths;
            set => _warrantyMonths = value;
        }

        [Required]
        public ProductStatus Status
        {
            get => _status;
            set => _status = value;
        }

        [Required]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        // Dynamic Pricing properties

        /// <summary>
        /// Giá vốn FIFO - bình quân của lô hàng cũ nhất đang tồn kho
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal CostFifo
        {
            get => _costFifo;
            set => _costFifo = value;
        }

        /// <summary>
        /// Giá thay thế NIFO - giá nhập dự kiến của lô hàng mới nhất
        /// </summary>
        [Range(0, double.MaxValue)]
        public decimal CostNifo
        {
            get => _costNifo;
            set => _costNifo = value;
        }

        /// <summary>
        /// Xu hướng thị trường (UP, DOWN, STABLE)
        /// </summary>
        public MarketTrend MarketTrend
        {
            get => _marketTrend;
            set => _marketTrend = value;
        }

        /// <summary>
        /// Chế độ định giá (AUTO_PROTECT hoặc CLEARANCE)
        /// </summary>
        public PricingMode PricingMode
        {
            get => _pricingMode;
            set => _pricingMode = value;
        }

        /// <summary>
        /// Thời điểm cập nhật giá gần nhất
        /// </summary>
        public DateTime? PriceUpdatedAt
        {
            get => _priceUpdatedAt;
            set => _priceUpdatedAt = value;
        }

        // Navigation: product has many attribute values
        public ICollection<ProductAttributeValue> ProductAttributeValues
        {
            get => _productAttributeValues;
            set => _productAttributeValues = value ?? new List<ProductAttributeValue>();
        }

        // Constructors
        public Product()
        {
            _id = 0;
            _sku = string.Empty;
            _name = string.Empty;
            _categoryId = 0;
            _modelId = 0;
            _brandId = null;
            _price = 0;
            _cost = 0;
            _isSerialTracked = false;
            _warrantyMonths = 12;
            _status = ProductStatus.ACTIVE;
            _createdAt = DateTime.UtcNow;
            _productAttributeValues = new List<ProductAttributeValue>();
            // Dynamic Pricing defaults
            _costFifo = 0;
            _costNifo = 0;
            _marketTrend = MarketTrend.STABLE;
            _pricingMode = PricingMode.AUTO_PROTECT;
            _priceUpdatedAt = null;
        }

        public Product(string sku, string name, int categoryId, decimal price)
        {
            _id = 0;
            _sku = sku ?? string.Empty;
            _name = name ?? string.Empty;
            _categoryId = categoryId;
            _modelId = 0;
            _brandId = null;
            _price = price;
            _cost = 0;
            _isSerialTracked = false;
            _warrantyMonths = 12;
            _status = ProductStatus.ACTIVE;
            _createdAt = DateTime.UtcNow;
            _productAttributeValues = new List<ProductAttributeValue>();
            // Dynamic Pricing defaults
            _costFifo = 0;
            _costNifo = 0;
            _marketTrend = MarketTrend.STABLE;
            _pricingMode = PricingMode.AUTO_PROTECT;
            _priceUpdatedAt = null;
        }
    }
}
