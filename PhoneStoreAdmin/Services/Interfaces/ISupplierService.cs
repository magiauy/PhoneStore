using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface ISupplierService 
    {
        Supplier? GetSupplierById(int supplierId);

        IEnumerable<Supplier> GetAll();

        SupplierResult GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 10);

        void ActivateSupplier(int supplierId);

        void DeactivateSupplier(int supplierId);
    }
}
