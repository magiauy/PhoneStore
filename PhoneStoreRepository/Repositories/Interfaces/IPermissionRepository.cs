using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Permission GetByCode(string code);

        // Async methods
        Task<Permission?> GetByIdAsync(int id);
        Task<Permission?> GetByCodeAsync(string code);
        Task<List<Permission>?> GetAllAsync();
        Task<Permission?> AddAsync(Permission entity);
        Task UpdateAsync(Permission entity);
        Task DeleteAsync(int id);
    }
}
