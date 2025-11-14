using PhoneStoreRepository.Models;
using PhoneStore.Services.ViewModels;
using System.Collections.Generic;

namespace PhoneStore.Services.Interfaces
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