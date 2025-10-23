using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IBrandService
    {
        Brand? GetBrandById(int brandId);
        bool Insert(Brand brand);
        bool Update(Brand brand);
        IEnumerable<Brand> GetAll();
        BrandResult GetBrandsFiltered(string? name, int page = 1, int pageSize = 10);
    }
}