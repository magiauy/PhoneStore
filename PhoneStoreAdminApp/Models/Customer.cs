using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class Customer : Person
    {
        [MaxLength(255)]
        public string? Address { get; set; }
    }
}
