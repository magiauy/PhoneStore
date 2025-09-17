using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Brand GetByName(string name);
    }
}
