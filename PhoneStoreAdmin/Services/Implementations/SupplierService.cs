using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _SupplierRepository;

        public SupplierService(ISupplierRepository SupplierRepository)
        {
            _SupplierRepository = SupplierRepository ?? throw new ArgumentNullException(nameof(SupplierRepository));
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int SupplierId)
        {
            try
            {
                Logger.Info($"Getting Supplier by ID: {SupplierId}");
                return await _SupplierRepository.GetByIdAsync(SupplierId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Supplier by ID: {SupplierId}", ex);
                return null;
            }
        }

    }
}