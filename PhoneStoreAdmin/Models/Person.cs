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
        [Key]
        public int Id { get; set; }

        [MaxLength(30)]
        public string? Code { get; set; }

        [Required]
        [MaxLength(120)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(20)]
        [Phone]
        public string? Phone { get; set; }

        [MaxLength(120)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public PersonType PersonType { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
