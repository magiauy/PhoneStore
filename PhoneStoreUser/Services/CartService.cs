using Microsoft.JSInterop;
using System.Text.Json;
using PhoneStoreUser.Components.Models;

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
                _cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
            }
            _isInitialized = true;
        }
        catch (JSException jsEx)
        {
            Log("JSException caught during cart initialization.");
            Log(jsEx.ToString());
            // JS runtime may not be available during prerendering, so defer initialization.
        }
        catch (Exception ex)
        {
            Log("Exception caught during cart initialization.");
            Log(ex.ToString());
            // Other errors should stop further retries to avoid infinite loops.
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

    public async Task AddToCart(Product product, string? imageUrl = null)
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

    public async Task RemoveFromCart(Product product)
    {
        await EnsureInitialized();
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem != null)
        {
            _cart.Remove(cartItem);
            await SaveCart();
        }
    }

    public async Task UpdateQuantity(Product product, int quantity)
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
