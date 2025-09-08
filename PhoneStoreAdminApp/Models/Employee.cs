using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class Employee : Person
    {
        public DateTime? HireDate { get; set; }
    }
}
