using PhoneStoreAdminApp.Models;

namespace PhoneStoreAdminApp.Repositories.Interfaces
{
    public interface IEmployeeRepository
    {
        Person GetByHireDate(DateTime hireDate);
    }
}
