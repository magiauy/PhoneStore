using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using MySqlConnector;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPurchaseOrderLineRepository : IRepository<PurchaseOrderLine>
    {
        ICollection<PurchaseOrderLine> GetByPurchaseOrderId(int purchaseOrderId);
        void DeleteByPurchaseOrderId(int purchaseOrderId);
        
        /// <summary>
        /// Insert a purchase order line using an existing transaction
        /// </summary>
        void Insert(PurchaseOrderLine entity, MySqlConnection connection, MySqlTransaction transaction);
    
        /// <summary>
        /// Delete purchase order lines by purchase order ID using an existing transaction
        /// </summary>
        void DeleteByPurchaseOrderId(int purchaseOrderId, MySqlConnection connection, MySqlTransaction transaction);
    }
}
