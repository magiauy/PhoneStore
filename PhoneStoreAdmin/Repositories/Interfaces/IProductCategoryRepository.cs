using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductCategoryRepository : IRepository<ProductCategory>
    {
        ProductCategory GetByName(string name);
        IEnumerable<ProductCategory> GetByParentId(int? parentId);
    }
}
