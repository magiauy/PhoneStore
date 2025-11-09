using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IProductModelRepository : IRepository<ProductModel>
    {
        ProductModel? FindBySlug(string slug);
        IEnumerable<ProductModel> SearchByName(string keyword);
    }
}
