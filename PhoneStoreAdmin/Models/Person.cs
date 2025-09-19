using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PhoneStoreAdmin.Models.Enums;

namespace PhoneStoreAdmin.Models
{
    [Table("persons")]
    public class Person
    {
        // Private backing fields
        private int _id;
        private string? _code;
        private string _fullName = string.Empty;
        private string? _phone;
        private string? _email;
        private PersonType _personType;
        private DateTime _createdAt = DateTime.UtcNow;
        private bool _isActive = true;
        private ICollection<Account> _accounts = new List<Account>();

        // Public properties with backing fields
        [Key]
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [MaxLength(30)]
        public string? Code
        {
            get => _code;
            set => _code = value;
        }

        [Required]
        [MaxLength(120)]
        public string FullName
        {
            get => _fullName;
            set => _fullName = value ?? string.Empty;
        }

        [MaxLength(20)]
        [Phone]
        public string? Phone
        {
            get => _phone;
            set => _phone = value;
        }

        [MaxLength(120)]
        [EmailAddress]
        public string? Email
        {
            get => _email;
            set => _email = value;
        }

        [Required]
        public PersonType PersonType
        {
            get => _personType;
            set => _personType = value;
        }

        [Required]
        public DateTime CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Navigation
        public ICollection<Account> Accounts
        {
            get => _accounts;
            set => _accounts = value ?? new List<Account>();
        }

        // Constructors
        public Person()
        {
            _id = 0;
            _code = null;
            _fullName = string.Empty;
            _phone = null;
            _email = null;
            _personType = PersonType.CUSTOMER; // Default value
            _createdAt = DateTime.UtcNow;
            _isActive = true;
            _accounts = new List<Account>();
        }

        public Person(string fullName, PersonType personType)
        {
            _id = 0;
            _code = null;
            _fullName = fullName ?? string.Empty;
            _phone = null;
            _email = null;
            _personType = personType;
            _createdAt = DateTime.UtcNow;
            _isActive = true;
            _accounts = new List<Account>();
        }
    }
}
