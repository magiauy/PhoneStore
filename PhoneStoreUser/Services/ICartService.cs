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
    
    /// <summary>
    /// Add product to cart with inventory check. Limits quantity to available stock.
    /// </summary>
    /// <param name="product">Product to add</param>
    /// <param name="quantity">Requested quantity</param>
    /// <param name="imageUrl">Optional image URL</param>
    /// <returns>Tuple with success flag and optional warning message if quantity was limited</returns>
    Task<(bool success, string? message)> AddToCartWithInventoryCheck(Product product, int quantity, string? imageUrl = null);
    
    /// <summary>
    /// Update cart quantity with inventory check. Limits quantity to available stock.
    /// </summary>
    /// <param name="product">Product to update</param>
    /// <param name="quantity">Requested new quantity</param>
    /// <returns>Tuple with success flag and optional warning message if quantity was limited</returns>
    Task<(bool success, string? message)> UpdateQuantityWithInventoryCheck(Product product, int quantity);
    
    /// <summary>
    /// Validate all cart items against current inventory at checkout.
    /// </summary>
    /// <returns>List of validation results for each cart item</returns>
    Task<List<CartValidationResult>> ValidateCartAtCheckout();
}

/// <summary>
/// Result of cart item validation against inventory
/// </summary>
public class CartValidationResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}
