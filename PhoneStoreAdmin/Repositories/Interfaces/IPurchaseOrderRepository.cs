using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        IEnumerable<PurchaseOrder> GetBySupplierId(int supplierId);
        IEnumerable<PurchaseOrder> GetByStatus(PhoneStoreAdmin.Models.Enums.PoStatus status);
        IEnumerable<PurchaseOrder> GetByCreatedBy(int createdBy);
    }
}
