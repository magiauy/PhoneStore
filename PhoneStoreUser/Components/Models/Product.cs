namespace PhoneStoreUser.Components.Models;

public record Product(
    int Id,
    string Sku,
    string Name,
    int CategoryId,
    int ModelId,
    int? BrandId = null,
    decimal Price = 0,
    decimal Cost = 0,
    bool IsSerialTracked = true,
    int WarrantyMonths = 12,
    string Status = "active",
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
