using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreRepository.Models;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface ICustomerService
    {
        /// <summary>
        /// Get customer by ID
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>Customer if found, null otherwise</returns>
        Task<Customer?> GetCustomerByIdAsync(int customerId);
        
        /// <summary>
        /// Get customer by ID (sync)
        /// </summary>
        Customer? GetCustomerById(int customerId);

        /// <summary>
        /// Get customer by email address
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Customer if found, null otherwise</returns>
        Task<Customer?> GetCustomerByEmailAsync(string email);

        /// <summary>
        /// Get customer by phone number
        /// </summary>
        /// <param name="phone">Phone number</param>
        /// <returns>Customer if found, null otherwise</returns>
        Task<Customer?> GetCustomerByPhoneAsync(string phone);

        /// <summary>
        /// Get customer by address
        /// </summary>
        /// <param name="address">Address</param>
        /// <returns>Customer if found, null otherwise</returns>
        Task<Customer?> GetCustomerByAddressAsync(string address);

        /// <summary>
        /// Get all customers
        /// </summary>
        /// <returns>List of customers</returns>
        Task<List<Customer>?> GetAllCustomersAsync();
        
        /// <summary>
        /// Get all customers (sync)
        /// </summary>
        IEnumerable<Customer> GetAll();
        
        /// <summary>
        /// Get customers filtered with pagination
        /// </summary>
        CustomerResult GetCustomersFiltered(string? searchTerm, CustomerFilterCriteria? filterCriteria = null, int page = 1, int pageSize = 10);

        /// <summary>
        /// Get customers filtered with pagination (async)
        /// </summary>
        Task<CustomerResult> GetCustomersFilteredAsync(string? searchTerm, CustomerFilterCriteria? filterCriteria = null, int page = 1, int pageSize = 10);

        /// <summary>
        /// Add new customer
        /// </summary>
        /// <param name="customer">Customer to add</param>
        /// <returns>Added customer if successful, null otherwise</returns>
        Task<Customer?> AddCustomerAsync(Customer customer);
        
        /// <summary>
        /// Insert new customer (sync)
        /// </summary>
        bool Insert(Customer customer);

        /// <summary>
        /// Update customer information
        /// </summary>
        /// <param name="customer">Customer to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateCustomerAsync(Customer customer);
        
        /// <summary>
        /// Update customer (sync)
        /// </summary>
        bool Update(Customer customer);

        /// <summary>
        /// Delete customer
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteCustomerAsync(int customerId);

        /// <summary>
        /// Check if a customer is active
        /// </summary>
        /// <param name="customerId">Customer ID</param>
        /// <returns>True if active, false otherwise</returns>
        Task<bool> IsCustomerActiveAsync(int customerId);
    }
}
