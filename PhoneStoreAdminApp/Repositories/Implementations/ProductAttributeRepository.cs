using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class ProductAttributeRepository(DataSource dataSource) : IProductAttributeRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductAttribute GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductAttribute> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(ProductAttribute entity)
        {
            throw new NotImplementedException();
        }

        public void Update(ProductAttribute entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public ProductAttribute GetByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
