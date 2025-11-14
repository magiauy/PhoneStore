using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreAdmin.ViewModels;
using System;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IPurchaseOrderService
    {
        PurchaseOrder? GetById(int id);

        void Insert(PurchaseOrder po);

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
    }
}