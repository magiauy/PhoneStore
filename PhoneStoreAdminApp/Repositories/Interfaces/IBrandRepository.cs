using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Brand GetByName(string name);
    }
}
