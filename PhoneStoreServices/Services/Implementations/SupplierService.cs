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
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        }

        public IEnumerable<Supplier> GetAll()
        {
            try
            {
                return _supplierRepository.GetAll();
            } catch (Exception ex)
            {
                Logger.Error("Failed to get Supplier all", ex);
                return Enumerable.Empty<Supplier>();
            }
        }

        public bool Insert(Supplier supplier)
        {
            try
            {
                _supplierRepository.Insert(supplier);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert Supplier", ex);
                return false;
            }
        }

        public bool Update(Supplier supplier)
        {
            try
            {
                _supplierRepository.Update(supplier);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update Supplier {supplier.Id}", ex);
                return false;
            }
        }

        public Supplier? GetSupplierById(int supplierId)
        {
            try
            {
                Logger.Info($"Getting Supplier by ID: {supplierId}");
                return _supplierRepository.GetById(supplierId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Supplier by ID: {supplierId}", ex);
                return null;
            }
        }

        public SupplierResult GetSuppliersFiltered(
            string? name,
            string? phone,
            string? email,
            string? address,
            string? taxNumber,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var suppliers = _supplierRepository.GetSuppliersFiltered(name, phone, email, address, taxNumber, isActive, page, pageSize);
                var totalPages = _supplierRepository.GetTotalPages(name, phone, email, address, taxNumber, isActive, pageSize);
                var totalRecords = _supplierRepository.GetTotalRecords(name, phone, email, address, taxNumber, isActive);
                return new SupplierResult(suppliers, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered suppliers", ex);
                return new SupplierResult(new Supplier[0], new InfoTable(0, 0));
            }
        }

        public void ActivateSupplier(int supplierId)
        {
            var supplier = GetSupplierById(supplierId);
            if (supplier != null && !supplier.IsActive)
            {
                supplier.IsActive = true;
                Update(supplier);
            }
        }

        public void DeactivateSupplier(int supplierId)
        {
            var supplier = GetSupplierById(supplierId);
            if (supplier != null && supplier.IsActive)
            {
                supplier.IsActive = false;
                Update(supplier);
            }
        }
    }
}