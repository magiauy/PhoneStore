using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Supplier GetByName(string name);
        void Activate(int id);
        void DeActivate(int id);
        IEnumerable<Supplier> GetSuppliersFiltered(string? name = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? taxNumber = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 10);
        int GetTotalPages(string? name = null,
            string? phone = null,
            string? email = null,
            string? address = null,
            string? taxNumber = null,
            bool? isActive = null,
            int page = 1,
            int pageSize = 10);
    }
}
