using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Supplier
    {
        // Private backing fields
        private int _id;
        private string _name = string.Empty;
        private string? _phone;
        private string? _email;
        private string? _address;
        private string? _taxNumber;
        private bool _isActive = true;

        // Public properties with backing fields
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required]
        [MaxLength(160)]
        public string Name
        {
            get => _name;
            set => _name = value ?? string.Empty;
        }

        [MaxLength(20)]
        [Phone]
        public string? Phone
        {
            get => _phone;
            set => _phone = value;
        }

        [MaxLength(120)]
        [EmailAddress]
        public string? Email
        {
            get => _email;
            set => _email = value;
        }

        [MaxLength(255)]
        public string? Address
        {
            get => _address;
            set => _address = value;
        }

        [MaxLength(50)]
        public string? TaxNumber
        {
            get => _taxNumber;
            set => _taxNumber = value;
        }

        [Required]
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        // Constructors
        public Supplier()
        {
            _id = 0;
            _name = string.Empty;
            _phone = null;
            _email = null;
            _address = null;
            _taxNumber = null;
            _isActive = true;
        }

        public Supplier(string name)
        {
            _id = 0;
            _name = name ?? string.Empty;
            _phone = null;
            _email = null;
            _address = null;
            _taxNumber = null;
            _isActive = true;
        }
    }

    public class InfoTable
    {
        public readonly int totalRecords;
        public readonly int totalPages;

        public InfoTable(int totalRecords, int totalPages)
        {
            this.totalRecords = totalRecords;
            this.totalPages = totalPages;
        }
    }

    public class SupplierResult
    {
        public IEnumerable<Supplier> Suppliers { get; set; }
        public InfoTable Info { get; set; }

        public SupplierResult(IEnumerable<Supplier> suppliers, InfoTable info)
        {
            Suppliers = suppliers;
            Info = info;
        }
    }
}