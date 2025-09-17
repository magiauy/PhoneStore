using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Data;
using PhoneStoreAdmin.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Implementations
{
    public class ProductSerialRepository(DataSource dataSource) : IProductSerialRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public ProductSerial GetById(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductSerial> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Insert(ProductSerial entity)
        {
            throw new NotImplementedException();
        }

        public void Update(ProductSerial entity)
        {
            throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            throw new NotImplementedException();
        }

        public ProductSerial GetBySerialNumber(string serialNumber)
        {
            throw new NotImplementedException();
        }

        public ProductSerial GetByImei1(string imei1)
        {
            throw new NotImplementedException();
        }

        public ProductSerial GetByImei2(string imei2)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductSerial> GetByProductId(int productId)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ProductSerial> GetByStatus(PhoneStoreAdmin.Models.Enums.SerialStatus status)
        {
            throw new NotImplementedException();
        }
    }
}
