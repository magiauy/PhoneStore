using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepository;

        public EmployeeService(IEmployeeRepository employeeRepository)
        {
            _employeeRepository = employeeRepository ?? throw new ArgumentNullException(nameof(employeeRepository));
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int employeeId)
        {
            try
            {
                Logger.Info($"Getting employee by ID: {employeeId}");
                var person = await _employeeRepository.GetByIdAsync(employeeId);
                return person as Employee;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get employee by ID: {employeeId}", ex);
                return null;
            }
        }

        public async Task<Employee?> GetEmployeeByHireDateAsync(DateTime hireDate)
        {
            try
            {
                Logger.Info($"Getting employee by hire date: {hireDate}");
                return await _employeeRepository.GetByHireDateAsync(hireDate);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get employee by hire date: {hireDate}", ex);
                return null;
            }
        }

        public async Task<Employee?> GetEmployeeByEmailAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return null;

                Logger.Info($"Getting employee by email: {email}");
                var person = await _employeeRepository.GetByEmailAsync(email);
                return person as Employee;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get employee by email: {email}", ex);
                return null;
            }
        }

        public async Task<Employee?> GetEmployeeByPhoneAsync(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return null;

                Logger.Info($"Getting employee by phone: {phone}");
                var person = await _employeeRepository.GetByPhoneAsync(phone);
                return person as Employee;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get employee by phone: {phone}", ex);
                return null;
            }
        }

        public async Task<List<Employee>?> GetAllEmployeesAsync()
        {
            try
            {
                Logger.Info("Getting all employees");
                return await _employeeRepository.GetAllEmployeesAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all employees", ex);
                return null;
            }
        }

        public async Task<List<Employee>?> GetEmployeesWithoutAccountAsync()
        {
            try
            {
                Logger.Info("Getting employees without accounts");
                return await _employeeRepository.GetEmployeesWithoutAccountAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get employees without accounts", ex);
                return null;
            }
        }

        public async Task<Employee?> AddEmployeeAsync(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Logger.Warning("Attempted to add null employee");
                    return null;
                }

                Logger.Info($"Adding new employee: {employee.FullName}");
                var addedPerson = await _employeeRepository.AddAsync(employee);
                return addedPerson as Employee;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to add employee: {employee?.FullName}", ex);
                return null;
            }
        }

        public async Task<bool> UpdateEmployeeAsync(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Logger.Warning("Attempted to update null employee");
                    return false;
                }

                Logger.Info($"Updating employee: {employee.Id} - {employee.FullName}");
                await _employeeRepository.UpdateAsync(employee);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update employee: {employee?.Id}", ex);
                return false;
            }
        }

        public async Task<bool> DeleteEmployeeAsync(int employeeId)
        {
            try
            {
                Logger.Info($"Deleting employee: {employeeId}");
                await _employeeRepository.DeleteAsync(employeeId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete employee: {employeeId}", ex);
                return false;
            }
        }

        public async Task<bool> IsEmployeeActiveAsync(int employeeId)
        {
            try
            {
                var employee = await GetEmployeeByIdAsync(employeeId);
                return employee?.IsActive ?? false;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to check if employee is active for ID: {employeeId}", ex);
                return false;
            }
        }
    }
}
