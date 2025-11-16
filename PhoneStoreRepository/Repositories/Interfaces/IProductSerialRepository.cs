using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using MySqlConnector;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IProductSerialRepository : IRepository<ProductSerial>
    {
        ProductSerial GetBySerialNumber(string serialNumber);
        ProductSerial GetByImei1(string imei1);
        ProductSerial GetByImei2(string imei2);
        IEnumerable<ProductSerial> GetByProductId(int productId);
        IEnumerable<ProductSerial> GetPagedByProductId(int productId, int page, int pageSize, out int totalCount, int? batchId = null);
        IEnumerable<ProductSerial> GetByStatus(SerialStatus status);
        IDictionary<int, int> GetCountsByProductIds(IEnumerable<int> productIds);
        
        /// <summary>
        /// Insert a product serial using an existing transaction
        /// </summary>
        void Insert(ProductSerial entity, MySqlConnection connection, MySqlTransaction transaction);

        /// <summary>
        /// Try to get a product serial by serial number. Returns null if not found.
        /// </summary>
        ProductSerial? TryGetBySerialNumber(string serialNumber);

        /// <summary>
        /// Try to get a product serial by IMEI1. Returns null if not found.
        /// </summary>
        ProductSerial? TryGetByImei1(string imei1);

        /// <summary>
        /// Try to get a product serial by IMEI2. Returns null if not found.
        /// </summary>
        ProductSerial? TryGetByImei2(string imei2);

        /// <summary>
        /// Delete all product serials associated with a purchase order
        /// </summary>
        void DeleteByPurchaseOrderId(int purchaseOrderId, MySqlConnection connection, MySqlTransaction transaction);

        /// <summary>
        /// Update product serials status and batch for a purchase order line
        /// </summary>
        void UpdateStatusAndBatchByLine(int lineId, SerialStatus oldStatus, SerialStatus newStatus, int batchId, string note, MySqlConnection connection, MySqlTransaction transaction);
    }
}
