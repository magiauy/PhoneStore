using PhoneStoreRepository.Models;

namespace PhoneStoreUser.Components.ViewModels;

public record ProductVariantViewModel(Product Product, string BrandName)
{
    public int Id => Product.Id;
    public string Sku => Product.Sku;
    public string Name => Product.Name;
    public int CategoryId => Product.CategoryId;
    public int ModelId => Product.ModelId;
    public int? BrandId => Product.BrandId;
    public decimal Price => Product.Price;
    public decimal Cost => Product.Cost;
    public bool IsSerialTracked => Product.IsSerialTracked;
    public int WarrantyMonths => Product.WarrantyMonths;
    public string Status => Product.Status.ToString().ToLowerInvariant();
    public DateTime CreatedAt => Product.CreatedAt;
}
