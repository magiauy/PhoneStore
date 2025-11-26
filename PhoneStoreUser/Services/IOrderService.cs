using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IOrderService
{
    Task<int> CreateOrderAsync(int? personId, List<CartItem> items, CheckoutModel model, string paymentMethod, int? promotionCodeId = null, decimal discountAmount = 0);
    Task<(int invoiceId, string? paymentUrl)> CreateOrderWithPaymentAsync(int? personId, List<CartItem> items, CheckoutModel model, int? promotionCodeId = null, decimal discountAmount = 0);
    Task<List<InvoiceEntity>> GetOrdersAsync(int personId);
    Task<InvoiceEntity?> GetOrderAsync(int invoiceId);
}
