using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IBrandRepository : IRepository<Brand>
    {
        Brand GetByName(string name);
        IEnumerable<Brand> GetBrandsFiltered(string? name, int page, int pageSize);
        int GetTotalRecords(string? name);
        int GetTotalPages(string? name, int pageSize);
    }
}
