using PhoneStoreAdminApp.Models;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Product GetBySku(string sku);
        IEnumerable<Product> GetByCategoryId(int categoryId);
        IEnumerable<Product> GetByBrandId(int brandId);
        IEnumerable<Product> GetByStatus(PhoneStoreAdminApp.Models.Enums.ProductStatus status);
    }
}
