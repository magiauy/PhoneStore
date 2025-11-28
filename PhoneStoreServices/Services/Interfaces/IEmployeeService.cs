using PhoneStoreRepository.Models;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStore.Services.Interfaces
{
    public interface IEmployeeService
    {
        /// <summary>
        /// Get employee by ID
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetEmployeeByIdAsync(int employeeId);
        
        /// <summary>
        /// Get employee by ID (sync)
        /// </summary>
        Employee? GetEmployeeById(int employeeId);

        /// <summary>
        /// Get employee by hire date
        /// </summary>
        /// <param name="hireDate">Hire date</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetEmployeeByHireDateAsync(DateTime hireDate);

        /// <summary>
        /// Get employee by email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetEmployeeByEmailAsync(string email);

        /// <summary>
        /// Get employee by phone
        /// </summary>
        /// <param name="phone">Phone number</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetEmployeeByPhoneAsync(string phone);

        /// <summary>
        /// Get all employees
        /// </summary>
        /// <returns>List of all employees</returns>
        Task<List<Employee>?> GetAllEmployeesAsync();
        
        /// <summary>
        /// Get all employees (sync)
        /// </summary>
        IEnumerable<Employee> GetAll();
        
        /// <summary>
        /// Get employees filtered with pagination
        /// </summary>
        EmployeeResult GetEmployeesFiltered(string? searchTerm, int page = 1, int pageSize = 10, EmployeeFilterCriteria? filterCriteria = null);

        /// <summary>
        /// Get employees filtered with pagination (async)
        /// </summary>
        Task<EmployeeResult> GetEmployeesFilteredAsync(string? searchTerm, int page = 1, int pageSize = 10, EmployeeFilterCriteria? filterCriteria = null);

        /// <summary>
        /// Get employees who don't have any account
        /// </summary>
        /// <returns>List of employees without accounts</returns>
        Task<List<Employee>?> GetEmployeesWithoutAccountAsync();

        /// <summary>
        /// Add new employee
        /// </summary>
        /// <param name="employee">Employee to add</param>
        /// <returns>Added employee if successful, null otherwise</returns>
        Task<Employee?> AddEmployeeAsync(Employee employee);
        
        /// <summary>
        /// Insert new employee (sync)
        /// </summary>
        bool Insert(Employee employee);

        /// <summary>
        /// Update employee
        /// </summary>
        /// <param name="employee">Employee to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdateEmployeeAsync(Employee employee);
        
        /// <summary>
        /// Update employee (sync)
        /// </summary>
        bool Update(Employee employee);

        /// <summary>
        /// Delete employee
        /// </summary>
        /// <param name="employeeId">Employee ID to delete</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteEmployeeAsync(int employeeId);

        /// <summary>
        /// Check if employee is active
        /// </summary>
        /// <param name="employeeId">Employee ID</param>
        /// <returns>True if employee is active, false otherwise</returns>
        Task<bool> IsEmployeeActiveAsync(int employeeId);
    }
}
