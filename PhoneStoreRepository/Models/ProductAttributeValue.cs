using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class ProductAttributeValue
    {
        // Private backing fields
        private int _id;
        private int _productId;
        private int _attributeId;
        private int? _optionId;
        private string? _valueText;
        private decimal? _valueNumber;
        private DateTime? _valueDate;
        private bool? _valueBool;

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

        [Required]
        public int AttributeId
        {
            get => _attributeId;
            set => _attributeId = value;
        }

        public int? OptionId
        {
            get => _optionId;
            set => _optionId = value;
        }

        [MaxLength(255)]
        public string? ValueText
        {
            get => _valueText;
            set => _valueText = value;
        }

        public decimal? ValueNumber
        {
            get => _valueNumber;
            set => _valueNumber = value;
        }

        public DateTime? ValueDate
        {
            get => _valueDate;
            set => _valueDate = value;
        }

        public bool? ValueBool
        {
            get => _valueBool;
            set => _valueBool = value;
        }

        // Constructors
        public ProductAttributeValue()
        {
            _id = 0;
            _productId = 0;
            _attributeId = 0;
            _optionId = null;
            _valueText = null;
            _valueNumber = null;
            _valueDate = null;
            _valueBool = null;
        }

        public ProductAttributeValue(int productId, int attributeId)
        {
            _id = 0;
            _productId = productId;
            _attributeId = attributeId;
            _optionId = null;
            _valueText = null;
            _valueNumber = null;
            _valueDate = null;
            _valueBool = null;
        }
    }
}
