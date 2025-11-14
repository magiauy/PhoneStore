using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.ViewModels
{
    public class SupplierViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string LocalizedStatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return PhoneStore.Services.Helpers.LocalizationHelper.GetString(key);
            }
        }
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";

        public SupplierViewModel() { }

        public SupplierViewModel(Supplier supplier)
        {
            Id = supplier.Id;
            Name = supplier.Name ?? string.Empty;
            Phone = supplier.Phone ?? string.Empty;
            Email = supplier.Email ?? string.Empty;
            Address = supplier.Address ?? string.Empty;
            TaxNumber = supplier.TaxNumber ?? string.Empty;
            IsActive = supplier.IsActive;
        }
    }

    public class InfoTable
    {
        public int TotalRecords { get; }
        public int TotalPages { get; }

        public InfoTable(int totalRecords, int totalPages)
        {
            TotalRecords = totalRecords;
            TotalPages = totalPages;
        }
    }

    public class SupplierResult
    {
        public IEnumerable<SupplierViewModel> Suppliers { get; set; }
        public InfoTable Info { get; set; }

        public SupplierResult(IEnumerable<Supplier> suppliers, InfoTable info)
        {
            Suppliers = suppliers.Select(s => new SupplierViewModel(s)).ToList();
            Info = info;
        }
    }
}
