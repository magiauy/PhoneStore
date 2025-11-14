using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        IEnumerable<Payment> GetByInvoiceId(int invoiceId);
        IEnumerable<Payment> GetByMethod(PhoneStoreRepository.Models.Enums.PaymentMethod method);
    }
}
