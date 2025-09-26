using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Implementations;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<Supplier?> GetSupplierByIdAsync(int supplierId);
        Task<SupplierResult> GetSuppliersFilteredAsync(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 10);

        Task ActivateSupplierAsync(int supplierId);
        Task DeactivateSupplierAsync(int supplierId);
    }
}
