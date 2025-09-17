using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Role GetByName(string name);
    }
}
