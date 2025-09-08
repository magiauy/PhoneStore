using PhoneStoreAdminApp.Models;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        IEnumerable<Payment> GetByInvoiceId(int invoiceId);
        IEnumerable<Payment> GetByMethod(PhoneStoreAdminApp.Models.Enums.PaymentMethod method);
    }
}
