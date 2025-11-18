namespace PhoneStoreUser.Components.Models;

public class ProductModelData
{
    public static List<ProductModel> GetSampleProductModels() => new()
    {
        new ProductModel(
            Id: 1,
            Name: "Iphone 12 256gb",
            Slug: "iphone-12-256gb",
            Description: "Model: Iphone 12 256gb (Apple)",
            Image: "IPHONE-12-256GB-IPHONE-12-256GB-CHINH-HANG-VNA---XANH-DUONG.jpg",
            CreatedAt: new DateTime(2025, 11, 18, 12, 5, 24),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 5, 24)
        )
    };
}
