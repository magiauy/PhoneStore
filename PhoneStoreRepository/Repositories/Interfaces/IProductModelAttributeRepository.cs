using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductModelAttributeRepository
    {
        IEnumerable<int> GetAttributeIdsByModel(int modelId);
        void ReplaceAttributesForModel(int modelId, IEnumerable<int> attributeIds);
    }
}
