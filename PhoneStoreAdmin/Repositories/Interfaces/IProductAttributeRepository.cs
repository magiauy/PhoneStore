using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductAttributeRepository : IRepository<ProductAttribute>
    {
        ProductAttribute GetByName(string name);
    }
}
