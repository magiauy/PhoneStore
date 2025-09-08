using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Permission GetByCode(string code);
    }
}
