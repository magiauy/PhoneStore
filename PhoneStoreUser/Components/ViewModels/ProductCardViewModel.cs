using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Components.ViewModels
{
    public record ProductCardViewModel(
        int Id,
        string Sku,
        string Name,
        int CategoryId,
        int ModelId,
        int? BrandId,
        string BrandName,
        decimal Price,
        decimal Cost,
        bool IsSerialTracked,
        int WarrantyMonths,
        string Status,
        DateTime? CreatedAt,
        DateTime? UpdatedAt,
        ICollection<ProductAttributeValue>? ProductAttributeValues,
        string Slug,
        PricingMode PricingMode = PricingMode.AUTO_PROTECT,
        DateTime? PriceUpdatedAt = null
    );
}
