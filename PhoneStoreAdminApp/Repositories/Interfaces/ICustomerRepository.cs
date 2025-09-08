using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Person GetByAddress(string address);
    }
}
