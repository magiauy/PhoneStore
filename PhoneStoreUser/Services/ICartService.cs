using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface ICartService
{
    event Action OnChange;
    Task AddToCart(Product product);
    Task RemoveFromCart(Product product);
    Task UpdateQuantity(Product product, int quantity);
    Task<List<CartItem>> GetCartItems();
    Task ClearCart();
    Task<decimal> GetTotal();
}
