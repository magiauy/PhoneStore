using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductAttributeRepository : IRepository<ProductAttribute>
    {
        ProductAttribute GetByName(string name);
        IEnumerable<ProductAttribute> GetAttributesFiltered(string? name, AttributeDataType? dataType, int page, int pageSize);
        int GetTotalRecords(string? name, AttributeDataType? dataType);
        int GetTotalPages(string? name, AttributeDataType? dataType, int pageSize);
    }
}
