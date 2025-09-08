using PhoneStoreAdminApp.Models;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IProductCategoryRepository : IRepository<ProductCategory>
    {
        ProductCategory GetByName(string name);
        IEnumerable<ProductCategory> GetByParentId(int? parentId);
    }
}
