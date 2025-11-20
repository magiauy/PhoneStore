using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface ICartService
{
    event Action OnChange;
    Task AddToCart(ProductVariantViewModel product, string? imageUrl = null);
    Task RemoveFromCart(ProductVariantViewModel product);
    Task UpdateQuantity(ProductVariantViewModel product, int quantity);
    Task<List<CartItem>> GetCartItems();
    Task ClearCart();
    Task<decimal> GetTotal();
    bool IsInitialized { get; }
    Task EnsureInitialized();    // Hàm khởi tạo an toàn
}
