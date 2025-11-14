using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PhoneStoreRepository.Models.Enums;

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
        
        public Employee? GetEmployeeById(int employeeId)
        {
            try
            {
                Logger.Info($"Getting employee by ID (sync): {employeeId}");
                return GetEmployeeByIdAsync(employeeId).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get employee by ID (sync): {employeeId}", ex);
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
        
        public IEnumerable<Employee> GetAll()
        {
            try
            {
                Logger.Info("Getting all employees (sync)");
                var result = GetAllEmployeesAsync().GetAwaiter().GetResult();
                return result ?? new List<Employee>();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all employees (sync)", ex);
                return new List<Employee>();
            }
        }
        
        public EmployeeResult GetEmployeesFiltered(string? searchTerm, int page = 1, int pageSize = 10, EmployeeFilterCriteria? filterCriteria = null)
        {
            try
            {
                Logger.Info($"Getting employees filtered: search='{searchTerm}', page={page}, pageSize={pageSize}, filter='{filterCriteria?.GetCacheKey() ?? "none"}'");

                var allEmployees = GetAll().ToList();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    var searchLower = searchTerm.ToLower();
                    allEmployees = allEmployees.Where(e =>
                        (e.FullName?.ToLower().Contains(searchLower) ?? false) ||
                        (e.Email?.ToLower().Contains(searchLower) ?? false) ||
                        (e.Phone?.ToLower().Contains(searchLower) ?? false) ||
                        (e.Code?.ToLower().Contains(searchLower) ?? false)
                    ).ToList();
                }

                if (filterCriteria != null)
                {
                    switch (filterCriteria.Status)
                    {
                        case "Active":
                            allEmployees = allEmployees.Where(e => e.IsActive).ToList();
                            break;
                        case "Inactive":
                            allEmployees = allEmployees.Where(e => !e.IsActive).ToList();
                            break;
                    }

                    if (filterCriteria.HireDateFrom.HasValue)
                    {
                        var fromDate = filterCriteria.HireDateFrom.Value.Date;
                        allEmployees = allEmployees
                            .Where(e => e.HireDate.HasValue && e.HireDate.Value.Date >= fromDate)
                            .ToList();
                    }

                    if (filterCriteria.HireDateTo.HasValue)
                    {
                        var toDate = filterCriteria.HireDateTo.Value.Date;
                        allEmployees = allEmployees
                            .Where(e => e.HireDate.HasValue && e.HireDate.Value.Date <= toDate)
                            .ToList();
                    }

                    if (filterCriteria.HasEmail == true)
                    {
                        allEmployees = allEmployees
                            .Where(e => !string.IsNullOrWhiteSpace(e.Email))
                            .ToList();
                    }

                    if (filterCriteria.HasPhone == true)
                    {
                        allEmployees = allEmployees
                            .Where(e => !string.IsNullOrWhiteSpace(e.Phone))
                            .ToList();
                    }
                }

                var totalRecords = allEmployees.Count;
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

                // Apply pagination
                var employees = allEmployees
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                
                var info = new InfoTable(totalRecords, totalPages);
                
                return new EmployeeResult(employees, info);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get employees filtered", ex);
                return new EmployeeResult(new List<Employee>(), new InfoTable(0, 0));
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

                employee.PersonType = PersonType.EMPLOYEE;

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
        
        public bool Insert(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Logger.Warning("Attempted to insert null employee");
                    return false;
                }

                employee.PersonType = PersonType.EMPLOYEE;

                Logger.Info($"Inserting new employee (sync): {employee.FullName}");
                var result = AddEmployeeAsync(employee).GetAwaiter().GetResult();
                return result != null;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to insert employee: {employee?.FullName}", ex);
                return false;
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

                employee.PersonType = PersonType.EMPLOYEE;

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
        
        public bool Update(Employee employee)
        {
            try
            {
                if (employee == null)
                {
                    Logger.Warning("Attempted to update null employee (sync)");
                    return false;
                }

                employee.PersonType = PersonType.EMPLOYEE;

                Logger.Info($"Updating employee (sync): {employee.Id} - {employee.FullName}");
                return UpdateEmployeeAsync(employee).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update employee (sync): {employee?.Id}", ex);
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
