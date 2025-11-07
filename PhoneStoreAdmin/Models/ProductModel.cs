using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class ProductModel
    {
        private int _id;
        private string _name = string.Empty;
        private string _slug = string.Empty;
        private string? _description;
        private string? _defaultImageUrl;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime _updatedAt = DateTime.UtcNow;

        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(200)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [Required]
        [MaxLength(200)]
        public string Slug
        {
            get => _slug;
            set => _slug = value ?? string.Empty;
        }

        [MaxLength(500)]
        public string? Description
        {
            get => _description;
            set => _description = value;
        }

        [MaxLength(500)]
        public string? DefaultImageUrl
        {
            get => _defaultImageUrl;
            set => _defaultImageUrl = value;
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

        public ProductModel()
        {
            _id = 0;
            _name = string.Empty;
            _slug = string.Empty;
            _description = null;
            _defaultImageUrl = null;
            _createdAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
        }

        public ProductModel(string name, string slug)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _slug = slug ?? string.Empty;
            _description = null;
            _defaultImageUrl = null;
            _createdAt = DateTime.UtcNow;
            _updatedAt = DateTime.UtcNow;
        }
    }
}
