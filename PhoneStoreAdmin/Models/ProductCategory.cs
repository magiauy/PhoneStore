using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class ProductCategory
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;
        private int? _parentId;
        private string? _note;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(120)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        public int? ParentId
        {
            get => _parentId;
            set => _parentId = value;
        }

        [MaxLength(255)]
        public string? Note
        {
            get => _note;
            set => _note = value;
        }

        // Constructors
        public ProductCategory()
        {
            _id = 0;
            _name = string.Empty;
            _parentId = null;
            _note = null;
        }

        public ProductCategory(string name)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _parentId = null;
            _note = null;
        }

        public ProductCategory(string name, int? parentId, string? note = null)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _parentId = parentId;
            _note = note;
        }
    }
}
