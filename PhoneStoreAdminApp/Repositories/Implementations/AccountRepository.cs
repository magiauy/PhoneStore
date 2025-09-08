using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class AccountRepository (DataSource dataSource) : IAccountRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Account GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Account> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Account entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Account entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Account GetByUsername(string username)
        {
            throw new NotImplementedException();
        }
    }
}
