using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class PersonRepository(DataSource dataSource) : IPersonRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public virtual Person GetById(int id)
        {
            throw new NotImplementedException();
        }

        public virtual IEnumerable<Person> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Person entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Person entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public virtual Person GetByEmail(string email)
        {
            throw new NotImplementedException();
        }

        public virtual Person GetByPhone(string phone)
        {
            throw new NotImplementedException();
        }

    }
}
