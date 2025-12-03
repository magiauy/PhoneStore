namespace PhoneStoreUser.Components.Models;

/// <summary>
/// Chế độ định giá của sản phẩm
/// </summary>
public enum PricingMode
{
    /// <summary>
    /// Chế độ định giá mặc định, tự động bảo vệ dòng tiền khi thị trường tăng
    /// </summary>
    AUTO_PROTECT = 0,

    /// <summary>
    /// Chế độ xả hàng, áp dụng khi Admin phê duyệt cắt lỗ
    /// </summary>
    CLEARANCE = 1
}

public record Product(
    int Id,
    string Sku,
    string Name,
    int CategoryId,
    int ModelId,
    int? BrandId = null,
    string BrandName = "",
    decimal Price = 0,
    decimal Cost = 0,
    bool IsSerialTracked = true,
    int WarrantyMonths = 12,
    string Status = "active",
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null,
    PricingMode PricingMode = PricingMode.AUTO_PROTECT,
    DateTime? PriceUpdatedAt = null
);
