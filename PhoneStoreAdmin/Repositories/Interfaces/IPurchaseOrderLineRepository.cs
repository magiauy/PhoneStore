using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPurchaseOrderLineRepository : IRepository<PurchaseOrderLine>
    {
        ICollection<PurchaseOrderLine> GetByPurchaseOrderId(int purchaseOrderId);
        void DeleteByPurchaseOrderId(int purchaseOrderId);
    }
}
