using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPromotionRepository : IRepository<Promotion>
    {
        Promotion GetByName(string name);
    }
}
