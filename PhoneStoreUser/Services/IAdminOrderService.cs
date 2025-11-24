using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminOrderService
{
    Task<List<AdminOrderDto>> GetPendingOrdersAsync(string? statusFilter = null);
    Task<AdminOrderDetailDto?> GetOrderDetailAsync(int invoiceId);
    Task UpdateOrderStatusAsync(int invoiceId, string newStatus);
    Task CancelOrderAsync(int invoiceId, string reason);
    Task AddSerialToOrderLineAsync(int invoiceLineId, string? serialNumber, string? imei1, string? imei2);
    Task<List<string>> GetOrderLineSerialsAsync(int invoiceLineId);
}
