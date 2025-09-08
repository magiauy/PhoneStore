using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdminApp.Models
{
    public class AccountRole
    {
        [Required]
        public int AccountId { get; set; }

        [Required]
        public int RoleId { get; set; }
    }
}
