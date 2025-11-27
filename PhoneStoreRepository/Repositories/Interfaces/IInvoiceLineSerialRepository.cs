using System.Collections.Generic;
using PhoneStoreRepository.Models;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IInvoiceLineSerialRepository
    {
        void Insert(InvoiceLineSerial entity);
        void InsertRange(IEnumerable<InvoiceLineSerial> entities);
    }
}
