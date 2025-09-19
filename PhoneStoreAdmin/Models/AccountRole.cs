using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class AccountRole
    {
        // Private backing fields
        private int _accountId;
        private int _roleId;
        private Account _account = null!;
        private Role _role = null!;

        // Public properties with backing fields
        [Required]
        public int AccountId
        {
            get => _accountId;
            set => _accountId = value;
        }

        [Required]
        public int RoleId
        {
            get => _roleId;
            set => _roleId = value;
        }

        // Navigation properties
        public Account Account
        {
            get => _account;
            set => _account = value;
        }

        public Role Role
        {
            get => _role;
            set => _role = value;
        }

        // Constructors
        public AccountRole()
        {
            _accountId = 0;
            _roleId = 0;
            _account = null!;
            _role = null!;
        }

        public AccountRole(int accountId, int roleId)
        {
            _accountId = accountId;
            _roleId = roleId;
            _account = null!;
            _role = null!;
        }
    }
}
