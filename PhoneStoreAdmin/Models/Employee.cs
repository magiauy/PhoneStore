using System;
using System.ComponentModel.DataAnnotations;

namespace PhoneStoreAdmin.Models
{
    public class Employee : Person
    {
        public DateTime? HireDate { get; set; }
    }
}
