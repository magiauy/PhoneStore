using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Product GetBySku(string sku);
        IEnumerable<Product> GetByCategoryId(int categoryId);
        IEnumerable<Product> GetByBrandId(int brandId);
        IEnumerable<Product> GetByStatus(PhoneStoreAdmin.Models.Enums.ProductStatus status);
    }
}
