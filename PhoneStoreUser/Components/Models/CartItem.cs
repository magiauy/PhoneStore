using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Components.Models;

public class CartItem
{
    public CartItem()
    {
    }

    public CartItem(Product product, int quantity, string? imageUrl = null)
    {
        Product = product;
        Quantity = quantity;
        ImageUrl = imageUrl;
    }

    public Product Product { get; set; } = new Product(0, string.Empty, string.Empty, 0, 0);
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }

    public decimal TotalPrice => Product.Price * Quantity;
}
