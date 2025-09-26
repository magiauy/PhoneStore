using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Implementations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        // Sync methods
        Supplier GetById(int id);
        IEnumerable<Supplier> GetAll();
        void Insert(Supplier entity);
        void Update(Supplier entity);
        void Delete(int id);
        IEnumerable<Supplier> GetSuppliers(int page, int pageSize);

        // Async methods
        Task<IEnumerable<Supplier>> GetAllAsync();
        Task<Supplier?> GetByIdAsync(int id);
        Task<Supplier?> AddAsync(Supplier entity);
        Task UpdateAsync(Supplier entity);
        Task DeleteAsync(int id);

        // Updated signature to match SuppliersPage expectations
        Task<SupplierResult> GetSuppliersFiltered(
        string? name,
        string? phone,
        string? email,
        string? address,
        string? taxNumber,
        bool? isActive,
        int page = 1,
        int pageSize = 20);

        // Add methods needed for pagination
        Task<int> GetTotalPages(
            string? name, 
            string? phone, 
            string? email, 
            string? address, 
            string? taxNumber, 
            bool? isActive, 
            int pageSize);
    }
}
