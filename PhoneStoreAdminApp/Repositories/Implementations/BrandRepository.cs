using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class BrandRepository(DataSource dataSource) : IBrandRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Brand GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Brand> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Brand entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Brand entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Brand GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
