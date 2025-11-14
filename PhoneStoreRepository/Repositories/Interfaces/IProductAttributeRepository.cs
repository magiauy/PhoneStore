using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductAttributeRepository : IRepository<ProductAttribute>
    {
        ProductAttribute GetByName(string name);
        IDictionary<string, ProductAttribute> GetByNames(IEnumerable<string> names);
        IEnumerable<ProductAttribute> GetAttributesFiltered(string? name, AttributeDataType? dataType, int page, int pageSize);
        int GetTotalRecords(string? name, AttributeDataType? dataType);
        int GetTotalPages(string? name, AttributeDataType? dataType, int pageSize);
    }
}
