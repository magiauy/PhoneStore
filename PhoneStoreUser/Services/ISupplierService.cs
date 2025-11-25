using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services
{
    public interface ISupplierService
    {
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
