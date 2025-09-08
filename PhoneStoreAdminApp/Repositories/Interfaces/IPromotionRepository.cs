using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Promotion GetByName(string name);
    }
}
