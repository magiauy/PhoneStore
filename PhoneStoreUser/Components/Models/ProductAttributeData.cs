namespace PhoneStoreUser.Components.Models;

public class ProductAttributeData
{
    public static List<ProductAttribute> GetSampleProductAttributes() => new()
    {
        new ProductAttribute(
            Id: 1,
            Code: "color",
            Name: "Màu Sắc",
            Type: "text",
            CreatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908)
        ),
        new ProductAttribute(
            Id: 2,
            Code: "storage",
            Name: "Dung lượng lưu trữ",
            Type: "text",
            CreatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908)
        ),
        new ProductAttribute(
            Id: 3,
            Code: "ram",
            Name: "RAM",
            Type: "text",
            CreatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908)
        ),
        new ProductAttribute(
            Id: 4,
            Code: "variant",
            Name: "Phiên bản",
            Type: "text",
            CreatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 41, 15, 908)
        )
    };
}
