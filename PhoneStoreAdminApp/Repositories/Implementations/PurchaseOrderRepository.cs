using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class PurchaseOrderRepository(DataSource dataSource) : IPurchaseOrderRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public PurchaseOrder GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PurchaseOrder> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(PurchaseOrder entity)
        {
            throw new NotImplementedException();
        }

        public void Update(PurchaseOrder entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PurchaseOrder> GetBySupplierId(int supplierId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PurchaseOrder> GetByStatus(PhoneStoreAdminApp.Models.Enums.PoStatus status)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<PurchaseOrder> GetByCreatedBy(int createdBy)
        {
            throw new NotImplementedException();
        }
    }
}
