using System.ComponentModel.DataAnnotations;

namespace PhoneStoreRepository.Models
{
    public class Customer : Person
    {
        // Private backing field
        private string? _address;

        // Public property with backing field
        [MaxLength(255)]
        public string? Address
        {
            get => _address;
            set => _address = value;
        }

        // Constructors
        public Customer() : base()
        {
            _address = null;
        }

        public Customer(string fullName) : base(fullName, Enums.PersonType.CUSTOMER)
        {
            _address = null;
        }

        public Customer(string fullName, string? address) : base(fullName, Enums.PersonType.CUSTOMER)
        {
            _address = address;
        }
    }
}
