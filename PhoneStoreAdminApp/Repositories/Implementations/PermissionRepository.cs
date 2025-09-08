using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class PermissionRepository(DataSource dataSource) : IPermissionRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Permission GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Permission> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Permission entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Permission entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Permission GetByCode(string code)
        {
            throw new NotImplementedException();
        }
    }
}
