using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreRepository.Models;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface IEmployeeRepository : IPersonRepository
    {
        /// <summary>
        /// Get employee by hire date
        /// </summary>
        /// <param name="hireDate">Hire date</param>
        /// <returns>Employee if found, null otherwise</returns>
        Employee? GetByHireDate(DateTime hireDate);

        /// <summary>
        /// Get employee by hire date async
        /// </summary>
        /// <param name="hireDate">Hire date</param>
        /// <returns>Employee if found, null otherwise</returns>
        Task<Employee?> GetByHireDateAsync(DateTime hireDate);

        /// <summary>
        /// Get all employees async
        /// </summary>
        /// <returns>List of employees</returns>
        Task<List<Employee>?> GetAllEmployeesAsync();

        /// <summary>
        /// Get employees who don't have any account
        /// </summary>
        /// <returns>List of employees without accounts</returns>
        Task<List<Employee>?> GetEmployeesWithoutAccountAsync();
    }
}
