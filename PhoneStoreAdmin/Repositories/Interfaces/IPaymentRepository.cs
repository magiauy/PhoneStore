using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        IEnumerable<Payment> GetByInvoiceId(int invoiceId);
        IEnumerable<Payment> GetByMethod(PhoneStoreAdmin.Models.Enums.PaymentMethod method);
    }
}
