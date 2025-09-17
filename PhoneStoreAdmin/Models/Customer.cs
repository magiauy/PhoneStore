using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Customer : Person
    {
        [MaxLength(255)]
        public string? Address { get; set; }
    }
}
