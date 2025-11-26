using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services
{
    public interface ISupplierService
    {
        Task<List<SupplierEntity>> GetAllSuppliersAsync();
        Task<(IReadOnlyList<SupplierEntity> Items, int TotalCount)> SearchSuppliersAsync(string? keyword, int skip, int take);
        IEnumerable<Supplier> GetAll();
        Supplier? GetSupplierById(int id);
        bool Insert(Supplier supplier);
        bool Update(Supplier supplier);
        void ActivateSupplier(int id);
        void DeactivateSupplier(int id);
        (IEnumerable<Supplier> Items, int TotalCount, int TotalPages) GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 10);
    }
}
