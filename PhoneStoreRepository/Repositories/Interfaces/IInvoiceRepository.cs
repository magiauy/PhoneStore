using PhoneStoreRepository.Models;
using System;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        IEnumerable<Invoice> GetByCustomer(int customerId);
        IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to);
        IEnumerable<Invoice> GetByStatus(PhoneStoreRepository.Models.Enums.InvoiceStatus status);
        IEnumerable<Invoice> GetByCreatedBy(int createdBy);
    }
}
