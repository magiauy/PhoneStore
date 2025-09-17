using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPersonRepository : IRepository<Person>
    {
        Person GetByEmail(string email);
        Person GetByPhone(string phone);
    }
}
