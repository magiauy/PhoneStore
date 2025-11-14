using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductRepository : IRepository<Product>
    {
        Product GetBySku(string sku);
        IEnumerable<Product> GetByCategoryId(int categoryId);
        IEnumerable<Product> GetByBrandId(int brandId);
        IEnumerable<Product> GetByStatus(PhoneStoreRepository.Models.Enums.ProductStatus status);
        IEnumerable<Product> GetByModelId(int modelId);
    }
}
