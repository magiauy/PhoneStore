using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreRepository.Models;

namespace PhoneStoreRepository.Repositories.Interfaces
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

        /// <summary>
        /// Get customers with filtering and pagination at database level
        /// </summary>
        /// <param name="searchTerm">Search term for customer name (full_name only)</param>
        /// <param name="filterCriteria">Advanced filter criteria for status, date, phone, address, etc.</param>
        /// <param name="page">Page number (1-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Tuple of (customers list, total count)</returns>
        Task<(List<Customer> Customers, int TotalCount)> GetCustomersFilteredAsync(
            string? searchTerm, CustomerFilterCriteria? filterCriteria, int page, int pageSize);
    }
}
