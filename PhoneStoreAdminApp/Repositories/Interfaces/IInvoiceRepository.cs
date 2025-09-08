using PhoneStoreAdminApp.Models;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IInvoiceRepository : IRepository<Invoice>
    {
        IEnumerable<Invoice> GetByCustomer(int customerId);
        IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to);
        IEnumerable<Invoice> GetByStatus(PhoneStoreAdminApp.Models.Enums.InvoiceStatus status);
        IEnumerable<Invoice> GetByCreatedBy(int createdBy);
    }
}
