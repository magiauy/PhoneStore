using System;
using System.Collections.Generic;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        IEnumerable<Invoice> GetByCustomer(int customerId);
        IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to);
        IEnumerable<Invoice> GetByStatus(InvoiceStatus status);
        IEnumerable<Invoice> GetByCreatedBy(int createdBy);
        IEnumerable<Invoice> GetInvoicesFiltered(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int page = 1,
            int pageSize = 20
        );

        int GetTotalRecords(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount
        );

        int GetTotalPages(
            string? customerName,
            int? customerId,
            int? createdBy,
            InvoiceStatus? status,
            
            DateTime? fromDate,
            DateTime? toDate,
            decimal? minAmount,
            decimal? maxAmount,
            int pageSize
        );
    }
}

