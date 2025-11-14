using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductCategoryRepository : IRepository<ProductCategory>
    {
        ProductCategory GetByName(string name);
        IEnumerable<ProductCategory> GetByParentId(int? parentId);
        IEnumerable<ProductCategory> GetCategoriesFiltered(string? name, int? parentId, int page, int pageSize);
        int GetTotalRecords(string? name, int? parentId);
        int GetTotalPages(string? name, int? parentId, int pageSize);
    }
}
