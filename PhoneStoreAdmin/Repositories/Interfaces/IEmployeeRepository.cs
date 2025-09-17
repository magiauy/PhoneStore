using System;
using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Person GetByHireDate(DateTime hireDate);
    }
}
