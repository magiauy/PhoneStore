namespace PhoneStoreUser.Components.Models;

public class ProductData
{
    public static List<Product> GetSampleProducts() => new()
    {
        new Product(
            Id: 1,
            Sku: "IPHONE-12-256GB-IPHONE-12-256GB-CHINH-HANG-VNA---XANH-DUONG",
            Name: "iPhone 12 256GB Chính hãng (VN/A) - Xanh dương",
            CategoryId: 1,
            BrandId: 1,
            ModelId: 1,
            Price: 0,
            Cost: 0,
            IsSerialTracked: 1,
            WarrantyMonths: 12,
            Status: 1,
            CreatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275)
        ),
        new Product(
            Id: 2,
            Sku: "IPHONE-12-256GB-IPHONE-12-256GB-CHINH-HANG-VNA-XANH",
            Name: "iPhone 12 256GB Chính hãng (VN/A)-Xanh",
            CategoryId: 1,
            BrandId: 1,
            ModelId: 1,
            Price: 0,
            Cost: 0,
            IsSerialTracked: 1,
            WarrantyMonths: 12,
            Status: 1,
            CreatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275)
        ),
        new Product(
            Id: 3,
            Sku: "IPHONE-12-256GB-IPHONE-12-256GB-CHINH-HANG-VNA-EN",
            Name: "iPhone 12 256GB Chính hãng (VN/A)-Đen",
            CategoryId: 1,
            BrandId: 1,
            ModelId: 1,
            Price: 0,
            Cost: 0,
            IsSerialTracked: 1,
            WarrantyMonths: 12,
            Status: 1,
            CreatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275)
        ),
        new Product(
            Id: 4,
            Sku: "IPHONE-12-256GB-IPHONE-12-256GB-I-CHINH-HANG-VNA--TIM",
            Name: "iPhone 12 256GB I Chính hãng VN/A -Tím",
            CategoryId: 1,
            BrandId: 1,
            ModelId: 1,
            Price: 0,
            Cost: 0,
            IsSerialTracked: 1,
            WarrantyMonths: 12,
            Status: 1,
            CreatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275)
        ),
        new Product(
            Id: 5,
            Sku: "IPHONE-12-256GB-IPHONE-12-256GB-CHINH-HANG-VNA-O",
            Name: "iPhone 12 256GB Chính hãng (VN/A)-Đỏ",
            CategoryId: 1,
            BrandId: 1,
            ModelId: 1,
            Price: 0,
            Cost: 0,
            IsSerialTracked: 1,
            WarrantyMonths: 12,
            Status: 1,
            CreatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275),
            UpdatedAt: new DateTime(2025, 11, 18, 12, 20, 33, 275)
        )
    };
}
