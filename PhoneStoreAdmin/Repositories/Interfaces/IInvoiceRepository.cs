using PhoneStoreAdmin.Models;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        IEnumerable<Invoice> GetByCustomer(int customerId);
        IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to);
        IEnumerable<Invoice> GetByStatus(PhoneStoreAdmin.Models.Enums.InvoiceStatus status);
        IEnumerable<Invoice> GetByCreatedBy(int createdBy);
    }
}
