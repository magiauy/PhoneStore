namespace PhoneStoreUser.Components.Models;

public record Product(
    int Id,
    string Sku,
    string Name,
    int CategoryId,
    int BrandId,
    int ModelId,
    decimal Price = 0,
    decimal Cost = 0,
    int IsSerialTracked = 1,
    int WarrantyMonths = 12,
    int Status = 1,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
