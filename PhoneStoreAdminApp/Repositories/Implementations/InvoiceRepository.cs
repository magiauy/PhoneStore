using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
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

        public IEnumerable<Invoice> GetByStatus(PhoneStoreAdminApp.Models.Enums.InvoiceStatus status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Invoice> GetByCreatedBy(int createdBy)
        {
            throw new NotImplementedException();
        }
    }
}
