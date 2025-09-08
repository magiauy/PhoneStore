using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class ProductCategoryRepository(DataSource dataSource) : IProductCategoryRepository
    {
        private readonly DataSource _dataSource = dataSource;

        public ProductCategory GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductCategory> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(ProductCategory entity)
        {
            throw new NotImplementedException();
        }

        public void Update(ProductCategory entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public ProductCategory GetByName(string name)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductCategory> GetByParentId(int? parentId)
        {
            throw new NotImplementedException();
        }
    }
}
