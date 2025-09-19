using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Employee : Person
    {
        // Private backing field
        private DateTime? _hireDate;

        // Public property with backing field
        public DateTime? HireDate
        {
            get => _hireDate;
            set => _hireDate = value;
        }

        // Constructors
        public Employee() : base()
        {
            _hireDate = null;
        }

        public Employee(string fullName) : base(fullName, Enums.PersonType.EMPLOYEE)
        {
            _hireDate = null;
        }

        public Employee(string fullName, DateTime hireDate) : base(fullName, Enums.PersonType.EMPLOYEE)
        {
            _hireDate = hireDate;
        }
    }
}
