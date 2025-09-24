using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ISupplierRepository : IRepository<Supplier>
    {
        Supplier GetByName(string name);
    }
}
