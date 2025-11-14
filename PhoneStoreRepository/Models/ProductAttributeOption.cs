using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class ProductAttributeOption
    {
        private int _id;
        private int _attributeId;
        private string _displayValue = string.Empty;
        private string? _normalizedValue;
        private int _sortOrder;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        public int AttributeId
        {
            get => _attributeId;
            set => _attributeId = value;
        }

        [Required]
        [MaxLength(200)]
        public string DisplayValue
        {
            get => _displayValue;
            set => _displayValue = value ?? string.Empty;
        }

        [MaxLength(200)]
        public string? NormalizedValue
        {
            get => _normalizedValue;
            set => _normalizedValue = value;
        }

        public int SortOrder
        {
            get => _sortOrder;
            set => _sortOrder = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        [Required]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        [Required]
        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => _updatedAt = value;
        }

        public ProductAttributeOption()
        {
            _id = 0;
            _attributeId = 0;
            _displayValue = string.Empty;
            _normalizedValue = null;
            _sortOrder = 0;
            _isActive = true;
            _createdAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
        }

        public ProductAttributeOption(int attributeId, string displayValue)
        {
            _id = 0;
            _attributeId = attributeId;
            _displayValue = displayValue ?? string.Empty;
            _normalizedValue = null;
            _sortOrder = 0;
            _isActive = true;
            _createdAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
        }
    }
}
