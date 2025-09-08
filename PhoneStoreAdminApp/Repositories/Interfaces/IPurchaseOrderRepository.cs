using PhoneStoreAdminApp.Models;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        IEnumerable<PurchaseOrder> GetBySupplierId(int supplierId);
        IEnumerable<PurchaseOrder> GetByStatus(PhoneStoreAdminApp.Models.Enums.PoStatus status);
        IEnumerable<PurchaseOrder> GetByCreatedBy(int createdBy);
    }
}
