using Microsoft.JSInterop;
using System.Text.Json;
using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public class CartService : ICartService
{
    private List<CartItem> _cart = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly IInventoryService _inventoryService;
    private bool _isInitialized = false;
    public bool IsInitialized => _isInitialized;

    public event Action OnChange;

#if DEBUG
    private void Log(string message)
    {
        // Console.WriteLine($"[CartService] {message}");
    }
#else
    private void Log(string message) { }
#endif

    public CartService(IJSRuntime jsRuntime, IInventoryService inventoryService)
    {
        _jsRuntime = jsRuntime;
        _inventoryService = inventoryService;
    }

    public async Task EnsureInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        // Check if JS interop is available (not prerendering)
        if (_jsRuntime is not IJSInProcessRuntime)
        {
            Log("JS interop not available (prerendering or server-side), skipping localStorage load.");
            _cart = new List<CartItem>();
            _isInitialized = true;
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
        catch (JSException jsEx)
        {
            Log("JSException caught: " + jsEx.Message);
            _cart = new List<CartItem>();
            _isInitialized = true;
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

    public async Task<(bool success, string? message)> AddToCartWithInventoryCheck(Product product, int quantity, string? imageUrl = null)
    {
        await EnsureInitialized();
        
        if (quantity <= 0)
        {
            return (false, "Số lượng phải lớn hơn 0");
        }

        // Get current availability for this product
        var availability = await _inventoryService.GetAvailabilityForProductsAsync(new[] { product.Id });
        var availableQuantity = availability.TryGetValue(product.Id, out var snapshot) 
            ? snapshot.AvailableQuantity 
            : 0;

        if (availableQuantity <= 0)
        {
            return (false, "Sản phẩm đã hết hàng");
        }

        // Check current cart quantity for this product
        var existingItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        var currentCartQuantity = existingItem?.Quantity ?? 0;
        var totalRequestedQuantity = currentCartQuantity + quantity;

        string? warningMessage = null;
        var actualQuantityToAdd = quantity;

        if (totalRequestedQuantity > availableQuantity)
        {
            // Limit to available stock
            actualQuantityToAdd = Math.Max(0, availableQuantity - currentCartQuantity);
            
            if (actualQuantityToAdd <= 0)
            {
                return (false, $"Bạn đã có {currentCartQuantity} sản phẩm trong giỏ hàng. Chỉ còn {availableQuantity} sản phẩm trong kho.");
            }
            
            warningMessage = $"Chỉ còn {availableQuantity} sản phẩm trong kho. Đã thêm {actualQuantityToAdd} sản phẩm vào giỏ hàng.";
        }

        // Add to cart
        if (existingItem == null)
        {
            _cart.Add(new CartItem(product, actualQuantityToAdd, imageUrl));
        }
        else
        {
            existingItem.Quantity += actualQuantityToAdd;
            if (!string.IsNullOrEmpty(imageUrl))
            {
                existingItem.ImageUrl = imageUrl;
            }
        }
        
        await SaveCart();
        return (true, warningMessage);
    }

    public async Task<(bool success, string? message)> UpdateQuantityWithInventoryCheck(Product product, int quantity)
    {
        await EnsureInitialized();
        
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem == null)
        {
            return (false, "Sản phẩm không có trong giỏ hàng");
        }

        if (quantity <= 0)
        {
            // Remove item from cart
            _cart.Remove(cartItem);
            await SaveCart();
            return (true, null);
        }

        // Get current availability for this product
        var availability = await _inventoryService.GetAvailabilityForProductsAsync(new[] { product.Id });
        var availableQuantity = availability.TryGetValue(product.Id, out var snapshot) 
            ? snapshot.AvailableQuantity 
            : 0;

        string? warningMessage = null;
        var actualQuantity = quantity;

        if (quantity > availableQuantity)
        {
            if (availableQuantity <= 0)
            {
                // Product is out of stock, remove from cart
                _cart.Remove(cartItem);
                await SaveCart();
                return (false, "Sản phẩm đã hết hàng và đã được xóa khỏi giỏ hàng");
            }
            
            // Limit to available stock
            actualQuantity = availableQuantity;
            warningMessage = $"Chỉ còn {availableQuantity} sản phẩm trong kho";
        }

        cartItem.Quantity = actualQuantity;
        await SaveCart();
        return (true, warningMessage);
    }

    public async Task<List<CartValidationResult>> ValidateCartAtCheckout()
    {
        await EnsureInitialized();
        
        var results = new List<CartValidationResult>();
        
        if (_cart.Count == 0)
        {
            return results;
        }

        // Get availability for all products in cart
        var productIds = _cart.Select(i => i.Product.Id).ToList();
        var availability = await _inventoryService.GetAvailabilityForProductsAsync(productIds);

        foreach (var item in _cart)
        {
            var availableQuantity = availability.TryGetValue(item.Product.Id, out var snapshot) 
                ? snapshot.AvailableQuantity 
                : 0;

            var result = new CartValidationResult
            {
                ProductId = item.Product.Id,
                ProductName = item.Product.Name,
                RequestedQuantity = item.Quantity,
                AvailableQuantity = availableQuantity
            };

            if (availableQuantity <= 0)
            {
                result.IsValid = false;
                result.Message = "Sản phẩm đã hết hàng";
            }
            else if (item.Quantity > availableQuantity)
            {
                result.IsValid = false;
                result.Message = $"Chỉ còn {availableQuantity} sản phẩm trong kho";
            }
            else
            {
                result.IsValid = true;
            }

            results.Add(result);
        }

        return results;
    }
}
