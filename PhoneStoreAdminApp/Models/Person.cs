using System.ComponentModel.DataAnnotations;
using PhoneStoreAdminApp.Models.Enums;

namespace PhoneStoreAdminApp.Models
{
    public class Person
    {
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

        public DateTime? CreatedAt { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
