using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class RoleRepository(DataSource dataSource) : IRoleRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Role GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Role> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Role entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Role entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Role GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
