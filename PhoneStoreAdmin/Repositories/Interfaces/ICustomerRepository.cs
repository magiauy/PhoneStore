using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Person GetByAddress(string address);
    }
}
