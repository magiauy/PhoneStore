using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Supplier GetByName(string name);
    }
}
