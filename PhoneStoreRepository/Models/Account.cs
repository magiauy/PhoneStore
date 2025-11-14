using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PhoneStoreRepository.Models
{
    [Table("accounts")]
    public class Account
    {
        // Private backing fields
        private int _id;
        private string _username = string.Empty;
        private string _passwordHash = string.Empty;
        private int _personId;
        private Person _person = null!;
        private bool _isActive = true;
        private DateTime _createdAt = DateTime.UtcNow;
        private DateTime? _lastLogin;
        private ICollection<AccountRole> _accountRoles = new List<AccountRole>();

        // Public properties with backing fields
        [Key]
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(50)]
        public string Username
        {
            get => _username;
            set => _username = value ?? string.Empty;
        }

        [Required]
        [MaxLength(255)]
        public string PasswordHash
        {
            get => _passwordHash;
            set => _passwordHash = value ?? string.Empty;
        }

        [Required]
        public int PersonId
        {
            get => _personId;
            set => _personId = value;
        }

        [ForeignKey("PersonId")]
        public Person Person
        {
            get => _person;
            set => _person = value;
        }

        [Required]
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

        public DateTime? LastLogin
        {
            get => _lastLogin;
            set => _lastLogin = value;
        }

        public ICollection<AccountRole> AccountRoles
        {
            get => _accountRoles;
            set => _accountRoles = value ?? new List<AccountRole>();
        }

        // Constructor
        public Account()
        {
            _id = 0;
            _username = string.Empty;
            _passwordHash = string.Empty;
            _personId = 0;
            _person = null!;
            _isActive = true;
            _createdAt = DateTime.UtcNow;
            _lastLogin = null;
            _accountRoles = new List<AccountRole>();
        }

        // Constructor with parameters
        public Account(string username, string passwordHash, int personId)
        {
            _id = 0;
            _username = username ?? string.Empty;
            _passwordHash = passwordHash ?? string.Empty;
            _personId = personId;
            _person = null!;
            _isActive = true;
            _createdAt = DateTime.UtcNow;
            _lastLogin = null;
            _accountRoles = new List<AccountRole>();
        }
    }
}
