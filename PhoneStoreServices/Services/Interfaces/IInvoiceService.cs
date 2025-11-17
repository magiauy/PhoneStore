using System;
using PhoneStore.Services.ViewModels;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStore.Services.Interfaces
{
    public interface IInvoiceService
    {
        Invoice? GetById(int id);

        void Insert(Invoice invoice);

        void Update(Invoice invoice);

        void Delete(int id);

        InvoiceResult GetInvoicesFiltered(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,

            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 10);

        int CountAll();

        void MarkAsPaid(int id);

        void CancelInvoice(int id);
    }
}
