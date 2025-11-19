using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public class CartService : ICartService
{
    private List<CartItem> _cart = new();

    public event Action OnChange;

    public CartService()
    {
        SeedSampleData();
    }

    private void SeedSampleData()
    {
        _cart.Add(new CartItem(new Product(1, "IP15PM-256-TI", "iPhone 15 Pro Max 256GB Titan Tự Nhiên", 1, 1, 1, "Apple", 29990000), 1));
        _cart.Add(new CartItem(new Product(2, "S24U-512-GR", "Samsung Galaxy S24 Ultra 512GB Xám Titan", 1, 2, 2, "Samsung", 31990000), 2));
        _cart.Add(new CartItem(new Product(3, "X14U-512-BK", "Xiaomi 14 Ultra 512GB Đen", 1, 3, 3, "Xiaomi", 24990000), 1));
    }

    public Task AddToCart(Product product)
    {
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem == null)
        {
            _cart.Add(new CartItem(product, 1));
        }
        else
        {
            cartItem.Quantity++;
        }
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task RemoveFromCart(Product product)
    {
        var cartItem = _cart.FirstOrDefault(i => i.Product.Id == product.Id);
        if (cartItem != null)
        {
            _cart.Remove(cartItem);
            OnChange?.Invoke();
        }
        return Task.CompletedTask;
    }

    public Task UpdateQuantity(Product product, int quantity)
    {
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
            OnChange?.Invoke();
        }
        return Task.CompletedTask;
    }

    public Task<List<CartItem>> GetCartItems()
    {
        return Task.FromResult(_cart);
    }

    public Task ClearCart()
    {
        _cart.Clear();
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public Task<decimal> GetTotal()
    {
        return Task.FromResult(_cart.Sum(i => i.TotalPrice));
    }
}
