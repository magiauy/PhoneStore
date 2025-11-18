namespace PhoneStoreUser.Components.Models;

public class ProductModelData
{
    public static List<ProductModel> GetSampleProductModels() => new()
    {
        new ProductModel(
            Id: 1,
            Name: "iPhone 12 256GB",
            Slug: "iphone-12-256gb",
            Description: "iPhone 12 256GB chính hãng VN/A",
            Image: "iphone-12-256gb.jpg",
            CreatedAt: new DateTime(2025, 11, 18, 12, 5, 24),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 5, 24)
        ),
        new ProductModel(
            Id: 2,
            Name: "Galaxy S25 Ultra",
            Slug: "samsung-galaxy-s25-ultra-512gb",
            Description: "Galaxy AI 512GB | Snapdragon 8 Gen 3",
            Image: "galaxy-s25-ultra.jpg",
            CreatedAt: new DateTime(2025, 2, 5, 9, 0, 0),
            UpdatedAt: new DateTime(2025, 2, 5, 9, 0, 0)
        ),
        new ProductModel(
            Id: 3,
            Name: "Xiaomi 14 Ultra",
            Slug: "xiaomi-14-ultra",
            Description: "Camera Leica, HyperOS",
            Image: "xiaomi-14-ultra.jpg",
            CreatedAt: new DateTime(2025, 3, 10, 11, 30, 0),
            UpdatedAt: new DateTime(2025, 3, 10, 11, 30, 0)
        ),
        new ProductModel(
            Id: 4,
            Name: "OPPO Find X7 Ultra",
            Slug: "oppo-find-x7-ultra",
            Description: "Zoom tiềm vọng ProXDR 1-120mm",
            Image: "oppo-find-x7-ultra.jpg",
            CreatedAt: new DateTime(2025, 1, 22, 8, 45, 0),
            UpdatedAt: new DateTime(2025, 1, 22, 8, 45, 0)
        )
    };
}
