using PhoneStoreAdmin.Models;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface ISupplierService
    {
        /// <summary>
        /// Get Supplier by ID
        /// </summary>
        /// <param name="SupplierId">Supplier ID</param>
        /// <returns>Supplier if found, null otherwise</returns>
        Task<Supplier?> GetSupplierByIdAsync(int SupplierId);

    }
}