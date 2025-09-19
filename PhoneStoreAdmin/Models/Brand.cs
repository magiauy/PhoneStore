using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Brand
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(100)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        // Constructors
        public Brand()
        {
            _id = 0;
            _name = string.Empty;
        }

        public Brand(string name)
        {
            _id = 0;
            _name = name ?? string.Empty;
        }
    }
}
