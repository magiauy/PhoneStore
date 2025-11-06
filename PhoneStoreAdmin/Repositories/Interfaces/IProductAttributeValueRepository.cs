using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductAttributeValueRepository : IRepository<ProductAttributeValue>
    {
        IEnumerable<ProductAttributeValue> GetByProductId(int productId);
        IEnumerable<ProductAttributeValue> GetByAttributeId(int attributeId);
    }
}
