using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Implementations;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
    {
        IEnumerable<PurchaseOrder> GetPurchaseOrdersFiltered(
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
            int pageSize = 20
        );

        int GetTotalRecords(
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount
        );

        int GetTotalPages(
            string? supplierName,
            int? supplierId,
            int? createdBy,
            PoStatus? status,
            string? note,
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int pageSize
        );
    }
}
