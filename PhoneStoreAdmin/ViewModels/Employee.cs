using PhoneStoreRepository.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class EmployeeViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string HireDate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        
        public string LocalizedStatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return PhoneStoreAdmin.Helpers.LocalizationHelper.GetString(key);
            }
        }
        
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";

        public EmployeeViewModel() { }

        public EmployeeViewModel(Employee employee)
        {
            Id = employee.Id;
            Code = employee.Code ?? string.Empty;
            FullName = employee.FullName ?? string.Empty;
            Phone = employee.Phone ?? string.Empty;
            Email = employee.Email ?? string.Empty;
            HireDate = employee.HireDate?.ToString("dd/MM/yyyy") ?? string.Empty;
            IsActive = employee.IsActive;
        }
    }

    public class EmployeeResult
    {
        public IEnumerable<EmployeeViewModel> Employees { get; set; }
        public InfoTable Info { get; set; }

        public EmployeeResult(IEnumerable<Employee> employees, InfoTable info)
        {
            Employees = employees.Select(e => new EmployeeViewModel(e)).ToList();
            Info = info;
        }
    }
}
