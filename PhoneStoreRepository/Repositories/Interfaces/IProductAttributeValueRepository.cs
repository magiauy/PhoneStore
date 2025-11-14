using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductAttributeValueRepository : IRepository<ProductAttributeValue>
    {
        IEnumerable<ProductAttributeValue> GetByProductId(int productId);
        IEnumerable<ProductAttributeValue> GetByAttributeId(int attributeId);
    }
}
