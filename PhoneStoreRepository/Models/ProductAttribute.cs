using System.ComponentModel.DataAnnotations;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Models
{
    public class ProductAttribute
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;
        private AttributeDataType _dataType = AttributeDataType.TEXT;
        private string? _note;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(80)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [Required]
        public AttributeDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Constructors
        public ProductAttribute()
        {
            _id = 0;
            _name = string.Empty;
            _dataType = AttributeDataType.TEXT;
            _note = null;
        }

        public ProductAttribute(string name, AttributeDataType dataType = AttributeDataType.TEXT)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _dataType = dataType;
            _note = null;
        }
    }
}
