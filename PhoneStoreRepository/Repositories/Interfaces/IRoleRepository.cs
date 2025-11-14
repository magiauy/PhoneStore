using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Role GetByName(string name);

        // Async methods
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByNameAsync(string name);
        Task<List<Role>?> GetAllAsync();
        Task<Role?> AddAsync(Role entity);
        Task UpdateAsync(Role entity);
        Task DeleteAsync(int id);
        Task<bool> UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
        Task<List<Permission>?> GetRolePermissionsAsync(int roleId);
    }
}
