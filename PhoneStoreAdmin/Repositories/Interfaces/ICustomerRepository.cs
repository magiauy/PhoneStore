using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface ICustomerRepository : IPersonRepository
    {
        /// <summary>
        /// Get customer by address (synchronous)
        /// </summary>
        /// <param name="address">Customer address</param>
        /// <returns>Customer if found, null otherwise</returns>
        Customer? GetByAddress(string address);

        /// <summary>
        /// Get customer by address asynchronously
        /// </summary>
        /// <param name="address">Customer address</param>
        /// <returns>Customer if found, null otherwise</returns>
        Task<Customer?> GetByAddressAsync(string address);

        /// <summary>
        /// Get all customers asynchronously
        /// </summary>
        /// <returns>List of customers</returns>
        Task<List<Customer>?> GetAllCustomersAsync();
    }
}
