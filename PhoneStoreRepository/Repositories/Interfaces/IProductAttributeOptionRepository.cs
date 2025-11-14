using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductAttributeOptionRepository : IRepository<ProductAttributeOption>
    {
        IEnumerable<ProductAttributeOption> GetByAttributeId(int attributeId);
        IDictionary<int, IReadOnlyList<ProductAttributeOption>> GetByAttributeIds(IEnumerable<int> attributeIds);
        ProductAttributeOption? FindByAttributeAndDisplay(int attributeId, string displayValue);
    }
}
