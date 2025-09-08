using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class PaymentRepository(DataSource dataSource) : IPaymentRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Payment GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Payment> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Payment entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Payment entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Payment> GetByInvoiceId(int invoiceId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Payment> GetByMethod(PhoneStoreAdminApp.Models.Enums.PaymentMethod method)
        {
            throw new NotImplementedException();
        }
    }
}
