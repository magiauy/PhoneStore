namespace PhoneStoreUser.Components.ViewModels;

public class CartItem
{
    public CartItem()
    {
    }

    public CartItem(ProductVariantViewModel product, int quantity, string? imageUrl = null)
    {
        Product = product;
        Quantity = quantity;
        ImageUrl = imageUrl;
    }

    public ProductVariantViewModel Product { get; set; } = new(new PhoneStoreRepository.Models.Product(), string.Empty);
    public int Quantity { get; set; }
    public string? ImageUrl { get; set; }

    public decimal TotalPrice => Product.Price * Quantity;
}
