using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class CustomerRepository(DataSource dataSource) : PersonRepository(dataSource), ICustomerRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public override Person GetById(int id)
        {
            // Viết logic riêng cho Customer
            // Ví dụ: SELECT * FROM persons WHERE id = @id AND person_type = 'CUSTOMER'
            throw new NotImplementedException();
        }

        public override IEnumerable<Person> GetAll()
        {
            // Viết logic riêng cho Customer
            // Ví dụ: SELECT * FROM persons WHERE person_type = 'CUSTOMER'
            throw new NotImplementedException();
        }

        public override Person GetByEmail(string email)
        {
            // Viết logic riêng cho Customer
            // Ví dụ: SELECT * FROM persons WHERE email = @email AND person_type = 'CUSTOMER'
            throw new NotImplementedException();
        }

        public override Person GetByPhone(string phone)
        {
            // Viết logic riêng cho Customer
            // Ví dụ: SELECT * FROM persons WHERE phone = @phone AND person_type = 'CUSTOMER'
            throw new NotImplementedException();
        }

        public Person GetByAddress(string address)
        {
            // Viết logic để lấy Customer theo địa chỉ
            // Ví dụ: SELECT * FROM persons WHERE address = @address AND person_type = 'CUSTOMER'
            throw new NotImplementedException();
        }
    }
}
