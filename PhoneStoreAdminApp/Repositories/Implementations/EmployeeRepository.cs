using PhoneStoreAdminApp.Data;
using PhoneStoreAdminApp.Models;
using PhoneStoreAdminApp.Repositories.Interfaces;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdminApp.Repositories.Implementations
{
    public class EmployeeRepository(DataSource dataSource) : PersonRepository(dataSource), IEmployeeRepository
    {
        private readonly DataSource _dataSource = dataSource;
        public override Person GetById(int id)
        {
            // Viết logic riêng cho Employee
            // Ví dụ: SELECT * FROM persons WHERE id = @id AND person_type = 'EMPLOYEE'
            throw new NotImplementedException();
        }

        public override IEnumerable<Person> GetAll()
        {
            // Viết logic riêng cho Employee
            // Ví dụ: SELECT * FROM persons WHERE person_type = 'EMPLOYEE'
            throw new NotImplementedException();
        }

        public override Person GetByEmail(string email)
        {
            // Viết logic riêng cho Employee
            // Ví dụ: SELECT * FROM persons WHERE email = @email AND person_type = 'EMPLOYEE'
            throw new NotImplementedException();
        }
        public override Person GetByPhone(string phone)
        {
            // Viết logic riêng cho Employee
            // Ví dụ: SELECT * FROM persons WHERE phone = @phone AND person_type = 'EMPLOYEE'
            throw new NotImplementedException();
        }

        public Person GetByHireDate(DateTime hireDate)
        {
            // Viết logic để lấy Employee theo ngày tuyển dụng
            // Ví dụ: SELECT * FROM persons WHERE hire_date = @hireDate AND person_type = 'EMPLOYEE'
            throw new NotImplementedException();
        }
    }
}
