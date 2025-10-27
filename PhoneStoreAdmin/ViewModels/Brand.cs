using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class BrandViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public BrandViewModel() { }

        public BrandViewModel(Brand brand)
        {
            Id = brand.Id;
            Name = brand.Name ?? string.Empty;
        }
    }

    public class BrandResult
    {
        public IEnumerable<BrandViewModel> Brands { get; set; }
        public InfoTable Info { get; set; }

        public BrandResult(IEnumerable<Brand> brands, InfoTable info)
        {
            Brands = brands.Select(b => new BrandViewModel(b)).ToList();
            Info = info;
        }
    }
}