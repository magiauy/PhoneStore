using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductModelRepository : IRepository<ProductModel>
    {
        ProductModel? FindBySlug(string slug);
        IEnumerable<ProductModel> SearchByName(string keyword);
    }
}
