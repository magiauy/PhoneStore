using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class InvoiceRepository(DataSource dataSource) : IInvoiceRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Invoice GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Invoice entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Invoice entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCustomer(int customerId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByDateRange(DateTime from, DateTime to)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByStatus(PhoneStoreAdmin.Models.Enums.InvoiceStatus status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCreatedBy(int createdBy)
        {
            throw new NotImplementedException();
        }
    }
}
