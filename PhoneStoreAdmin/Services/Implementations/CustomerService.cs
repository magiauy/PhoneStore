using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;

        public CustomerService(ICustomerRepository customerRepository)
        {
            _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));
        }

        public async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            try
            {
                Logger.Info($"Getting customer by ID: {customerId}");
                var person = await _customerRepository.GetByIdAsync(customerId);
                return person as Customer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get customer by ID: {customerId}", ex);
                return null;
            }
        }
        
        public Customer? GetCustomerById(int customerId)
        {
            try
            {
                Logger.Info($"Getting customer by ID (sync): {customerId}");
                return GetCustomerByIdAsync(customerId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get customer by ID (sync): {customerId}", ex);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                Logger.Info($"Getting customer by email: {email}");
                var person = await _customerRepository.GetByEmailAsync(email);
                return person as Customer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get customer by email: {email}", ex);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByPhoneAsync(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return null;

                Logger.Info($"Getting customer by phone: {phone}");
                var person = await _customerRepository.GetByPhoneAsync(phone);
                return person as Customer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get customer by phone: {phone}", ex);
                return null;
            }
        }

        public async Task<Customer?> GetCustomerByAddressAsync(string address)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return null;

                Logger.Info($"Getting customer by address: {address}");
                return await _customerRepository.GetByAddressAsync(address);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get customer by address: {address}", ex);
                return null;
            }
        }

        public async Task<List<Customer>?> GetAllCustomersAsync()
        {
            try
            {
                Logger.Info("Getting all customers");
                return await _customerRepository.GetAllCustomersAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all customers", ex);
                return null;
            }
        }
        
        public IEnumerable<Customer> GetAll()
        {
            try
            {
                Logger.Info("Getting all customers (sync)");
                var result = GetAllCustomersAsync().GetAwaiter().GetResult();
                return result ?? new List<Customer>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all customers (sync)", ex);
                return new List<Customer>();
            }
        }
        
        public CustomerResult GetCustomersFiltered(string? searchTerm, CustomerFilterCriteria? filterCriteria = null, int page = 1, int pageSize = 10)
        {
            try
            {
                Logger.Info($"Getting customers filtered: search='{searchTerm}', filter='{filterCriteria?.GetCacheKey()}', page={page}, pageSize={pageSize}");

                // Use async method and wait for result - this is now optimized at DB level
                var (customers, totalCount) = _customerRepository
                    .GetCustomersFilteredAsync(searchTerm, filterCriteria, page, pageSize)
                    .GetAwaiter()
                    .GetResult();
                
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var info = new InfoTable(totalCount, totalPages);
                
                Logger.Info($"Retrieved {customers.Count} customers out of {totalCount} total");
                return new CustomerResult(customers, info);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get customers filtered", ex);
                return new CustomerResult(new List<Customer>(), new InfoTable(0, 0));
            }
        }

        public async Task<CustomerResult> GetCustomersFilteredAsync(string? searchTerm, CustomerFilterCriteria? filterCriteria = null, int page = 1, int pageSize = 10)
        {
            try
            {
                Logger.Info($"Getting customers filtered async: search='{searchTerm}', filter='{filterCriteria?.GetCacheKey()}', page={page}, pageSize={pageSize}");

                var (customers, totalCount) = await _customerRepository.GetCustomersFilteredAsync(searchTerm, filterCriteria, page, pageSize);
                
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                var info = new InfoTable(totalCount, totalPages);
                
                Logger.Info($"Retrieved {customers.Count} customers out of {totalCount} total");
                return new CustomerResult(customers, info);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get customers filtered async", ex);
                return new CustomerResult(new List<Customer>(), new InfoTable(0, 0));
            }
        }

        public async Task<Customer?> AddCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    Logger.Warning("Attempted to add null customer");
                    return null;
                }

                customer.PersonType = PersonType.CUSTOMER;

                Logger.Info($"Adding new customer: {customer.FullName}");
                var addedPerson = await _customerRepository.AddAsync(customer);
                return addedPerson as Customer;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to add customer: {customer?.FullName}", ex);
                return null;
            }
        }
        
        public bool Insert(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    Logger.Warning("Attempted to insert null customer");
                    return false;
                }

                customer.PersonType = PersonType.CUSTOMER;

                Logger.Info($"Inserting new customer (sync): {customer.FullName}");
                var result = AddCustomerAsync(customer).GetAwaiter().GetResult();
                return result != null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to insert customer: {customer?.FullName}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    Logger.Warning("Attempted to update null customer");
                    return false;
                }

                customer.PersonType = PersonType.CUSTOMER;

                Logger.Info($"Updating customer: {customer.Id} - {customer.FullName}");
                await _customerRepository.UpdateAsync(customer);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update customer: {customer?.Id}", ex);
                return false;
            }
        }
        
        public bool Update(Customer customer)
        {
            try
            {
                if (customer == null)
                {
                    Logger.Warning("Attempted to update null customer (sync)");
                    return false;
                }

                customer.PersonType = PersonType.CUSTOMER;

                Logger.Info($"Updating customer (sync): {customer.Id} - {customer.FullName}");
                return UpdateCustomerAsync(customer).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update customer (sync): {customer?.Id}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteCustomerAsync(int customerId)
        {
            try
            {
                Logger.Info($"Deleting customer: {customerId}");
                await _customerRepository.DeleteAsync(customerId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete customer: {customerId}", ex);
                return false;
            }
        }

        public async Task<bool> IsCustomerActiveAsync(int customerId)
        {
            try
            {
                var customer = await GetCustomerByIdAsync(customerId);
                return customer?.IsActive ?? false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check if customer is active for ID: {customerId}", ex);
                return false;
            }
        }
    }
}
