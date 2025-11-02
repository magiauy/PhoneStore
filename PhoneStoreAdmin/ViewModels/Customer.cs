using PhoneStoreAdmin.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class CustomerViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        
        public string LocalizedStatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return PhoneStoreAdmin.Helpers.LocalizationHelper.GetString(key);
            }
        }
        
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
        
        public string CreatedAtText => CreatedAt.ToString("dd/MM/yyyy");

        public CustomerViewModel() { }

        public CustomerViewModel(Customer customer)
        {
            Id = customer.Id;
            Code = customer.Code ?? string.Empty;
            FullName = customer.FullName ?? string.Empty;
            Phone = customer.Phone ?? string.Empty;
            Email = customer.Email ?? string.Empty;
            Address = customer.Address ?? string.Empty;
            IsActive = customer.IsActive;
            CreatedAt = customer.CreatedAt;
        }
    }

    public class CustomerResult
    {
        public IEnumerable<CustomerViewModel> Customers { get; set; }
        public InfoTable Info { get; set; }

        public CustomerResult(IEnumerable<Customer> customers, InfoTable info)
        {
            Customers = customers.Select(c => new CustomerViewModel(c)).ToList();
            Info = info;
        }
    }
}