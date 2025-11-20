using Microsoft.JSInterop;
using System.Text.Json;
using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public class CartService : ICartService
{
    private List<CartItem> _cart = new();
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    public event Action OnChange;

#if DEBUG
    private void Log(string message)
    {
        Console.WriteLine($"[CartService] {message}");
    }
#else
    private void Log(string message) { }
#endif

    public CartService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

public async Task EnsureInitialized()
{
    if (_isInitialized)
    {
        return;
    }

    try
    {
        var cartJson = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "cart");
        Log("Loaded cart from localStorage." + cartJson);
        if (!string.IsNullOrEmpty(cartJson))
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson, options) ?? new List<CartItem>();
        }
        _isInitialized = true;
    }
    catch (InvalidOperationException ex) when (ex.Message.Contains("prerendering"))
    {
        // 👇 SỬA: Bỏ qua lỗi Prerendering một cách êm đẹp
        // Không log lỗi này vì nó là hành vi bình thường của Blazor Server khi chưa kết nối Browser
        Log("Prerendering: LocalStorage not available yet.");
    }
    catch (JSException jsEx)
    {
        Log("JSException caught: " + jsEx.Message);
    }
    catch (Exception ex)
    {
        Log("Error initializing cart: " + ex.Message);
        // Khởi tạo list rỗng để tránh crash app
        _cart = new List<CartItem>();
        _isInitialized = true;
    }
}
    private async Task SaveCart()
    {
        var cartJson = JsonSerializer.Serialize(_cart);
        Log($"cartJson: {cartJson}");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "cart", cartJson);
        OnChange?.Invoke();
    }

    public async Task AddToCart(ProductVariantViewModel product, string? imageUrl = null)
    {
        await EnsureInitialized();
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem == null)
        {
            _cart.Add(new CartItem(product, 1, imageUrl));
        }
        else
        {
            cartItem.Quantity++;
            // Optionally update image url if provided
            if (!string.IsNullOrEmpty(imageUrl))
            {
                cartItem.ImageUrl = imageUrl;
            }
        }
        await SaveCart();
    }

    public async Task RemoveFromCart(ProductVariantViewModel product)
    {
        await EnsureInitialized();
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem != null)
        {
            _cart.Remove(cartItem);
            await SaveCart();
        }
    }

    public async Task UpdateQuantity(ProductVariantViewModel product, int quantity)
    {
        await EnsureInitialized();
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem != null)
        {
            if (quantity > 0)
            {
                cartItem.Quantity = quantity;
            }
            else
            {
                _cart.Remove(cartItem);
            }
            await SaveCart();
        }
    }

    public async Task<List<CartItem>> GetCartItems()
    {
        await EnsureInitialized();
        return _cart;
    }

    public async Task ClearCart()
    {
        _cart.Clear();
        await SaveCart();
    }

    public async Task<decimal> GetTotal()
    {
        await EnsureInitialized();
        return _cart.Sum(i => i.TotalPrice);
    }
}
