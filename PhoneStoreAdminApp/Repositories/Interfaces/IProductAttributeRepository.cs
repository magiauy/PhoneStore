using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IProductAttributeRepository : IRepository<ProductAttribute>
    {
        ProductAttribute GetByName(string name);
    }
}
