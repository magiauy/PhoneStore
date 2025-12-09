using System.Collections.Generic;
using System.Threading.Tasks;
using PhoneStoreUser.Models;

namespace PhoneStoreUser.Services
{
    public interface IPurchaseOrderService
    {
        Task<List<PurchaseOrderDTO>> GetPurchaseOrdersAsync();
        Task<PurchaseOrderDTO?> GetPurchaseOrderByIdAsync(int id);
        Task<int> CreatePurchaseOrderAsync(PurchaseOrderDTO dto);
        Task UpdatePurchaseOrderAsync(PurchaseOrderDTO dto);
        Task CompletePurchaseOrderAsync(int id);
        Task CancelPurchaseOrderAsync(int id);
        Task<bool> IsImeiExistsAsync(string imei);
        Task<bool> IsSerialNumberExistsAsync(string serialNumber);
    }
}
