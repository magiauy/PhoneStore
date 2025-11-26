using System.Collections.Generic;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IInventoryService
{
    Task<InventoryPageResult> GetInventorySummaryAsync(int page, int pageSize, string? searchTerm = null);
    Task<List<BatchDetailDto>> GetProductBatchDetailsAsync(int productId);
    Task<Dictionary<int, ProductAvailabilitySnapshot>> GetAvailabilityForProductsAsync(IEnumerable<int> productIds);
}

public class InventoryPageResult
{
    public List<InventorySummaryDto> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class InventorySummaryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int TotalQuantity { get; set; }
    public bool IsSerialTracked { get; set; }
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
}

public class BatchDetailDto
{
    public int BatchId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ImportedQuantity { get; set; }
    public int CurrentQuantity { get; set; }
}

public class ProductAvailabilitySnapshot
{
    public int ProductId { get; set; }
    public bool IsSerialTracked { get; set; }
    public int AvailableQuantity { get; set; }
}
