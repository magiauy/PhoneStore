using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.Implementations
{
    public class BrandService : IBrandService
    {
        private readonly IBrandRepository _brandRepository;

        public BrandService(IBrandRepository brandRepository)
        {
            _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
        }

        public IEnumerable<Brand> GetAll()
        {
            try
            {
                return _brandRepository.GetAll();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get Brand all", ex);
                return Enumerable.Empty<Brand>();
            }
        }

        public bool Insert(Brand brand)
        {
            try
            {
                _brandRepository.Insert(brand);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert Brand", ex);
                return false;
            }
        }

        public bool Update(Brand brand)
        {
            try
            {
                _brandRepository.Update(brand);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update Brand {brand.Id}", ex);
                return false;
            }
        }

        public Brand? GetBrandById(int brandId)
        {
            try
            {
                Logger.Info($"Getting Brand by ID: {brandId}");
                return _brandRepository.GetById(brandId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Brand by ID: {brandId}", ex);
                return null;
            }
        }

        public BrandResult GetBrandsFiltered(string? name, int page = 1, int pageSize = 10)
        {
            try
            {
                var brands = _brandRepository.GetBrandsFiltered(name, page, pageSize);
                var totalPages = _brandRepository.GetTotalPages(name, pageSize);
                var totalRecords = _brandRepository.GetTotalRecords(name);
                return new BrandResult(brands, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered brands", ex);
                return new BrandResult(new Brand[0], new InfoTable(0, 0));
            }
        }
    }
}