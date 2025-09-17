using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IAccountRepository : IRepository<Account>
    {
        Account GetByUsername(string username);
    }
}
