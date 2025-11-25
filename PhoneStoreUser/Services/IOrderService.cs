using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IOrderService
{
    Task<int> CreateOrderAsync(int? personId, List<CartItem> items, CheckoutModel model, string paymentMethod);
    Task<(int invoiceId, string? paymentUrl)> CreateOrderWithPaymentAsync(int? personId, List<CartItem> items, CheckoutModel model);
    Task<List<InvoiceEntity>> GetOrdersAsync(int personId);
}
