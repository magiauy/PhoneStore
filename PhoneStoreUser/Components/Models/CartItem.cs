using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Components.Models;

public class CartItem
{
    public Product Product { get; set; }
    public int Quantity { get; set; }

    public decimal TotalPrice => Product.Price * Quantity;

    public CartItem(Product product, int quantity)
    {
        Product = product;
        Quantity = quantity;
    }
}
