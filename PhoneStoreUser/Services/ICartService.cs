using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface ICartService
{
    event Action OnChange;
    Task AddToCart(Product product, string? imageUrl = null);
    Task RemoveFromCart(Product product);
    Task UpdateQuantity(Product product, int quantity);
    Task<List<CartItem>> GetCartItems();
    Task ClearCart();
    Task<decimal> GetTotal();
    bool IsInitialized { get; }
    Task EnsureInitialized();    // Hàm khởi tạo an toàn
}
