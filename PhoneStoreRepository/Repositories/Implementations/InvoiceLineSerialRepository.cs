using System.Collections.Generic;
using MySqlConnector;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;

namespace PhoneStoreRepository.Repositories.Implementations
{
    public class InvoiceLineSerialRepository : IInvoiceLineSerialRepository
    {
        private readonly DataSource _dataSource;

        public InvoiceLineSerialRepository(DataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void Insert(InvoiceLineSerial entity)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                INSERT INTO invoice_line_serials (invoice_line_id, product_serial_id)
                VALUES (@invoiceLineId, @productSerialId)", connection);
            command.Parameters.AddWithValue("@invoiceLineId", entity.InvoiceLineId);
            command.Parameters.AddWithValue("@productSerialId", entity.ProductSerialId);
            command.ExecuteNonQuery();
        }

        public void InsertRange(IEnumerable<InvoiceLineSerial> entities)
        {
            using var connection = _dataSource.GetConnection();
            using var command = new MySqlCommand(@"
                INSERT INTO invoice_line_serials (invoice_line_id, product_serial_id)
                VALUES (@invoiceLineId, @productSerialId)", connection);

            var invoiceLineParam = command.Parameters.Add("@invoiceLineId", MySqlDbType.Int32);
            var productSerialParam = command.Parameters.Add("@productSerialId", MySqlDbType.Int32);

            foreach (var entity in entities)
            {
                invoiceLineParam.Value = entity.InvoiceLineId;
                productSerialParam.Value = entity.ProductSerialId;
                command.ExecuteNonQuery();
            }
        }
    }
}
