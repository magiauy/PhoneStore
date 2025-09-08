using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class SupplierRepository(DataSource dataSource) : ISupplierRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public Supplier GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Supplier> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Supplier entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Supplier entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Supplier GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
