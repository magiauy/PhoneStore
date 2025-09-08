using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class ProductRepository(DataSource dataSource) : IProductRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public Product GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(Product entity)
        {
            throw new NotImplementedException();
        }

        public void Update(Product entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public Product GetBySku(string sku)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByCategoryId(int categoryId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByBrandId(int brandId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Product> GetByStatus(PhoneStoreAdminApp.Models.Enums.ProductStatus status)
        {
            throw new NotImplementedException();
        }
    }
}
