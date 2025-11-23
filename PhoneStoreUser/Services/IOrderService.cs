using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IOrderService
{
    Task<int> CreateOrderAsync(int? personId, List<CartItem> items, CheckoutModel model, string paymentMethod);
    Task<List<InvoiceEntity>> GetOrdersAsync(int personId);
}
