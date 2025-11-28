using System.Collections.Generic;
using MySqlConnector;
using PhoneStoreRepository.Models;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IInvoiceLineSerialRepository
    {
        void Insert(InvoiceLineSerial entity);
        void InsertRange(IEnumerable<InvoiceLineSerial> entities);

        /// <summary>
        /// Insert invoice line serials using an existing transaction
        /// </summary>
        void InsertRange(IEnumerable<InvoiceLineSerial> entities, MySqlConnection connection, MySqlTransaction transaction);
    }
}
