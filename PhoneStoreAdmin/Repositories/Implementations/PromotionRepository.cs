using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class PromotionRepository(DataSource dataSource) : IPromotionRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Promotion GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Promotion> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Promotion entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Promotion entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Promotion GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
