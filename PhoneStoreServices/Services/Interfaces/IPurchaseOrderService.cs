using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;

namespace PhoneStore.Services.Interfaces
{
    public interface IPurchaseOrderService
    {
        PurchaseOrder? GetById(int id);

        /// <summary>
        /// Insert purchase order with product serials in single transaction
        /// </summary>
        /// <param name="po">Purchase order with lines</param>
        /// <param name="serialsByLineId">Dictionary mapping PurchaseOrderLine.Id to list of ProductSerials</param>
        void Insert(PurchaseOrder po, Dictionary<int, List<ProductSerial>>? serialsByLineId = null);

        void Update(PurchaseOrder po);

        void Delete(int id);

        PurchaseOrderResult GetPurchaseOrdersFiltered(
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 10);

        int CountAll();

        void MarkAsReceived(int id);

        void CancelOrder(int id);

        /// <summary>
        /// Add product serials to a purchase order line
        /// </summary>
        void AddProductSerials(int purchaseOrderId, int purchaseOrderLineId, List<ProductSerial> serials);
    }
}