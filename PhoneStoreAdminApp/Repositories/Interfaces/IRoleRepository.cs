using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Role GetByName(string name);
    }
}
