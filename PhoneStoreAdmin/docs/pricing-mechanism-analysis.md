# Pricing Mechanism Analysis

## Tổng quan

Tài liệu này phân tích chi tiết cơ chế định giá (Pricing Mechanism) trong hệ thống PhoneStoreAdmin, bao gồm cách tính giá bán từ giá nhập và biên độ lợi nhuận (Profit Margin).

## Kiến trúc định giá

### Luồng dữ liệu giá

```mermaid
flowchart LR
    subgraph "1. Nhập hàng"
        A[PurchaseOrderLine] --> B[UnitCost]
        A --> C[ProfitMargin]
    end
    
    subgraph "2. Tính giá bán"
        B --> D[SellingPrice = UnitCost × 1 + ProfitMargin]
        C --> D
    end
    
    subgraph "3. Lưu trữ"
        D --> E[BatchProduct.SellingPrice]
        B --> F[BatchProduct.CostPrice]
        C --> G[BatchProduct.ProfitMargin]
    end
    
    subgraph "4. Bán hàng"
        E --> H[InvoiceLine.UnitPrice]
    end
```

## Data Models liên quan đến giá

### PurchaseOrderLine - Giá nhập

```csharp
public class PurchaseOrderLine
{
    int Id                    // Primary key
    int PurchaseOrderId       // FK đến PurchaseOrder
    int ProductId             // FK đến Product
    int Quantity              // Số lượng
    decimal UnitCost          // Giá nhập/đơn vị (CostPrice)
    decimal TotalCost         // Tổng giá = Quantity × UnitCost
    float ProfitMargin        // Biên độ lợi nhuận (0.20 = 20%)
}
```

### BatchProduct - Giá trong lô hàng

```csharp
public class BatchProduct
{
    int Id                    // Primary key
    int BatchId               // FK đến Batches
    int ProductId             // FK đến Product
    int Quantity              // Số lượng
    decimal CostPrice         // Giá nhập (từ PurchaseOrderLine.UnitCost)
    decimal SellingPrice      // Giá bán (tính từ công thức)
    float ProfitMargin        // Biên độ lợi nhuận (từ PurchaseOrderLine)
}
```

## Công thức tính giá

### Công thức cơ bản

```
SellingPrice = CostPrice × (1 + ProfitMargin)
```

### Ví dụ tính toán

| CostPrice (VND) | ProfitMargin | SellingPrice (VND) | Profit/Unit (VND) |
|-----------------|--------------|--------------------|--------------------|
| 10,000,000 | 0.10 (10%) | 11,000,000 | 1,000,000 |
| 10,000,000 | 0.15 (15%) | 11,500,000 | 1,500,000 |
| 10,000,000 | 0.20 (20%) | 12,000,000 | 2,000,000 |
| 10,000,000 | 0.25 (25%) | 12,500,000 | 2,500,000 |
| 10,000,000 | 0.30 (30%) | 13,000,000 | 3,000,000 |

### Code Implementation

```csharp
// Trong PurchaseOrderService.MarkAsReceived()
var sellingPrice = line.UnitCost * (1 + (decimal)line.ProfitMargin);

var batchProduct = new BatchProduct
{
    BatchId = batch.id,
    ProductId = line.ProductId,
    Quantity = line.Quantity,
    CostPrice = line.UnitCost,
    SellingPrice = sellingPrice,
    ProfitMargin = line.ProfitMargin
};
```

## System Settings - Minimum Profit Margin

### Cấu hình hệ thống

Hệ thống có setting `PROFIT_MARGIN` để quy định biên độ lợi nhuận tối thiểu:

```csharp
// Trong SettingStringService
private static readonly Dictionary<SystemSettingCode, (string Value, string Type)> DefaultSettings = new()
{
    { SystemSettingCode.PROFIT_MARGIN, ("0.20", "number") },  // Default 20%
};
```

### Sử dụng trong AddPurchaseOrderPage

```csharp
private float _minimumProfitMargin = 0.2f; // Default 20%

private void LoadMinimumProfitMargin()
{
    try
    {
        var profitMarginStr = SettingStringService.GetValue(SystemSettingCode.PROFIT_MARGIN, "0.20");
        if (float.TryParse(profitMarginStr, System.Globalization.NumberStyles.Float, 
            System.Globalization.CultureInfo.InvariantCulture, out var profitMargin))
        {
            _minimumProfitMargin = profitMargin;
        }
    }
    catch (Exception ex)
    {
        _minimumProfitMargin = 0.2f; // Fallback to default
    }
}
```

### Validation khi tạo PO

```csharp
// Trong OnCreatePurchaseOrder()
var invalidProfitMarginItems = PurchaseOrderItems
    .Where(item => item.ProfitMargin < _minimumProfitMargin)
    .Select(item => $"{item.ProductName}: {item.ProfitMargin:P0} (minimum: {_minimumProfitMargin:P0})")
    .ToList();

if (invalidProfitMarginItems.Any())
{
    var errorMessage = $"Profit margin must be >= {_minimumProfitMargin:P0} (system setting):\n\n" +
        string.Join("\n", invalidProfitMarginItems);
    // Show error dialog
    return;
}
```

## Flowchart - Quy trình định giá

```mermaid
flowchart TD
    subgraph "1. Cấu hình hệ thống"
        A[Admin cấu hình PROFIT_MARGIN] --> B[Lưu vào setting_strings table]
        B --> C[Default: 0.20 = 20%]
    end

    subgraph "2. Tạo đơn đặt hàng"
        D[User tạo PurchaseOrder] --> E[Nhập UnitCost cho mỗi sản phẩm]
        E --> F[Nhập ProfitMargin cho mỗi sản phẩm]
        F --> G{ProfitMargin >= Minimum?}
        G -->|No| H[Hiển thị lỗi validation]
        H --> F
        G -->|Yes| I[Tính TotalCost = Quantity × UnitCost]
        I --> J[Lưu PurchaseOrderLine]
    end

    subgraph "3. Nhận hàng"
        K[User click Receive] --> L[Tạo Batch]
        L --> M[Loop: Mỗi PurchaseOrderLine]
        M --> N[Tính SellingPrice = UnitCost × 1 + ProfitMargin]
        N --> O[Tạo BatchProduct với CostPrice, SellingPrice, ProfitMargin]
        O --> P{Còn line?}
        P -->|Yes| M
        P -->|No| Q[Hoàn tất]
    end

    subgraph "4. Bán hàng"
        R[User tạo Invoice] --> S[Lấy SellingPrice từ BatchProduct]
        S --> T[Gán vào InvoiceLine.UnitPrice]
        T --> U[Tính TotalPrice = Quantity × UnitPrice × 1 - DiscountPct]
    end

    J --> K
    Q --> R
```

## Mối quan hệ giữa các entities về giá

```mermaid
erDiagram
    PurchaseOrderLine ||--o| BatchProduct : "creates"
    BatchProduct ||--o{ InvoiceLine : "provides_price"
    
    PurchaseOrderLine {
        decimal UnitCost "Giá nhập"
        float ProfitMargin "Biên độ lợi nhuận"
        decimal TotalCost "Quantity × UnitCost"
    }
    
    BatchProduct {
        decimal CostPrice "= PurchaseOrderLine.UnitCost"
        decimal SellingPrice "= CostPrice × 1 + ProfitMargin"
        float ProfitMargin "= PurchaseOrderLine.ProfitMargin"
    }
    
    InvoiceLine {
        decimal UnitPrice "= BatchProduct.SellingPrice"
        decimal DiscountPct "Giảm giá %"
        decimal TotalPrice "Quantity × UnitPrice × 1 - DiscountPct"
    }
```

## Các điểm lưu trữ giá trong hệ thống

| Entity | Field | Mô tả | Nguồn |
|--------|-------|-------|-------|
| PurchaseOrderLine | UnitCost | Giá nhập/đơn vị | User nhập |
| PurchaseOrderLine | ProfitMargin | Biên độ lợi nhuận | User nhập (>= minimum) |
| PurchaseOrderLine | TotalCost | Tổng giá nhập | Quantity × UnitCost |
| BatchProduct | CostPrice | Giá nhập | = PurchaseOrderLine.UnitCost |
| BatchProduct | SellingPrice | Giá bán | = CostPrice × (1 + ProfitMargin) |
| BatchProduct | ProfitMargin | Biên độ lợi nhuận | = PurchaseOrderLine.ProfitMargin |
| InvoiceLine | UnitPrice | Giá bán/đơn vị | = BatchProduct.SellingPrice |
| InvoiceLine | DiscountPct | Giảm giá % | User nhập hoặc từ Promotion |
| InvoiceLine | TotalPrice | Tổng tiền | Quantity × UnitPrice × (1 - DiscountPct) |

## Tính toán lợi nhuận

### Gross Profit per Unit

```
GrossProfit = SellingPrice - CostPrice
            = CostPrice × (1 + ProfitMargin) - CostPrice
            = CostPrice × ProfitMargin
```

### Gross Profit per Batch

```
BatchGrossProfit = Σ (SellingPrice - CostPrice) × QuantitySold
```

### Ví dụ

```
Batch có 10 sản phẩm:
- CostPrice = 10,000,000 VND
- ProfitMargin = 0.15 (15%)
- SellingPrice = 11,500,000 VND

Nếu bán hết 10 sản phẩm:
- Total Revenue = 10 × 11,500,000 = 115,000,000 VND
- Total Cost = 10 × 10,000,000 = 100,000,000 VND
- Gross Profit = 115,000,000 - 100,000,000 = 15,000,000 VND
```

## UI hiển thị giá

### AddPurchaseOrderPage

| Field | Hiển thị | Format |
|-------|----------|--------|
| UnitCost | TextBox | Currency (VND) |
| ProfitMargin | TextBox/Slider | Percentage (%) |
| Calculated SellingPrice | TextBlock (readonly) | Currency (VND) |

### BatchesPage (Detail Dialog)

| Field | Hiển thị | Format |
|-------|----------|--------|
| CostPrice | TextBlock | Currency (VND) |
| SellingPrice | TextBlock (bold) | Currency (VND) |
| ProfitMargin | TextBlock | Percentage (%) |

### SalesPage

| Field | Hiển thị | Format |
|-------|----------|--------|
| Price (SellingPrice) | TextBlock | Currency (VND) |
| StockQuantity | TextBlock | Number |

## Validation Rules

### ProfitMargin Validation

```csharp
// Rule 1: ProfitMargin phải >= 0
[Range(0, 1)]
public float ProfitMargin { get; set; }

// Rule 2: ProfitMargin phải >= MinimumProfitMargin (từ system settings)
if (item.ProfitMargin < _minimumProfitMargin)
{
    // Show error
}
```

### UnitCost Validation

```csharp
// UnitCost phải > 0
[Required]
[Range(0, double.MaxValue)]
public decimal UnitCost { get; set; }
```

## Sequence Diagram - Định giá khi nhận hàng

```mermaid
sequenceDiagram
    participant User
    participant PurchaseOrdersPage
    participant PurchaseOrderService
    participant BatchProductRepository
    participant Database

    User->>PurchaseOrdersPage: Click Receive
    PurchaseOrdersPage->>PurchaseOrderService: MarkAsReceived(poId)
    
    PurchaseOrderService->>PurchaseOrderService: GetById(poId)
    Note over PurchaseOrderService: Load PO with Lines
    
    PurchaseOrderService->>Database: BEGIN TRANSACTION
    PurchaseOrderService->>Database: INSERT batches
    
    loop Mỗi PurchaseOrderLine
        Note over PurchaseOrderService: line.UnitCost = 10,000,000
        Note over PurchaseOrderService: line.ProfitMargin = 0.15
        
        PurchaseOrderService->>PurchaseOrderService: Calculate SellingPrice
        Note over PurchaseOrderService: sellingPrice = 10,000,000 × 1.15 = 11,500,000
        
        PurchaseOrderService->>BatchProductRepository: Insert(batchProduct)
        Note over BatchProductRepository: CostPrice = 10,000,000
        Note over BatchProductRepository: SellingPrice = 11,500,000
        Note over BatchProductRepository: ProfitMargin = 0.15
        
        BatchProductRepository->>Database: INSERT batch_products
    end
    
    PurchaseOrderService->>Database: COMMIT
    PurchaseOrderService-->>PurchaseOrdersPage: Success
```

## Kết luận

Cơ chế định giá trong PhoneStoreAdmin có các đặc điểm:

1. **Công thức đơn giản**: SellingPrice = CostPrice × (1 + ProfitMargin)
2. **Minimum Profit Margin**: Có thể cấu hình qua system settings
3. **Lưu trữ đầy đủ**: CostPrice, SellingPrice, ProfitMargin được lưu trong BatchProduct
4. **Validation**: Kiểm tra ProfitMargin >= minimum trước khi lưu PO

### Điểm mạnh
- Công thức tính giá rõ ràng, dễ hiểu
- Có thể cấu hình minimum profit margin
- Lưu trữ đầy đủ thông tin giá để tính toán lợi nhuận

### Điểm cần cải thiện
- Thêm tính năng bulk update profit margin cho nhiều sản phẩm
- Hiển thị profit margin dạng % trong UI (hiện tại lưu dạng decimal)
- Thêm báo cáo profit analysis theo batch/product
- Hỗ trợ pricing rules phức tạp hơn (tiered pricing, volume discount)
