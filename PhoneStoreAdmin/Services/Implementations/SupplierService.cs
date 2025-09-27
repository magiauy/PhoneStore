using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Implementations;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Threading.Tasks;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int supplierId)
        {
            try
            {
                Logger.Info($"Getting Supplier by ID: {supplierId}");
                return await _supplierRepository.GetByIdAsync(supplierId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Supplier by ID: {supplierId}", ex);
                return null;
            }
        }

        public async Task<SupplierResult> GetSuppliersFilteredAsync(
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
                var suppliers = await _supplierRepository.GetSuppliersFiltered(name, phone, email, address, taxNumber, isActive, page, pageSize);
                var totalPages = await _supplierRepository.GetTotalPages(name, phone, email, address, taxNumber, isActive, pageSize);
                var totalRecords = await _supplierRepository.GetTotalRecords(name, phone, email, address, taxNumber, isActive);
                return new SupplierResult(suppliers, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered suppliers", ex);
                return new SupplierResult(new Supplier[0], new InfoTable(0, 0));
            }
        }

        public async Task ActivateSupplierAsync(int supplierId)
        {
            var supplier = await GetSupplierByIdAsync(supplierId);
            if (supplier != null && !supplier.IsActive)
            {
                supplier.IsActive = true;
                await _supplierRepository.UpdateAsync(supplier);
            }
        }

        public async Task DeactivateSupplierAsync(int supplierId)
        {
            var supplier = await GetSupplierByIdAsync(supplierId);
            if (supplier != null && supplier.IsActive)
            {
                supplier.IsActive = false;
                await _supplierRepository.UpdateAsync(supplier);
            }
        }
    }
}
