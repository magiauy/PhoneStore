using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Permission GetByCode(string code);
    }
}
