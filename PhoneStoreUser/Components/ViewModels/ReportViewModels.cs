namespace PhoneStoreUser.Components.ViewModels;

/// <summary>
/// Chart data point for visualizations
/// </summary>
public record ChartDataPoint(
    string Label,
    decimal Value,
    int Count
);

/// <summary>
/// Purchase order report item
/// </summary>
public record PurchaseOrderReportItem(
    int Id,
    DateTime OrderDate,
    string SupplierName,
    int TotalItems,
    decimal TotalValue,
    string Status
);

/// <summary>
/// Purchase order report data with summary and details
/// </summary>
public record PurchaseOrderReportData(
    int TotalCount,
    decimal TotalValue,
    List<ChartDataPoint> ChartData,
    List<PurchaseOrderReportItem> Items
);

/// <summary>
/// Invoice report item
/// </summary>
public record InvoiceReportItem(
    int Id,
    DateTime InvoiceDate,
    string CustomerName,
    int TotalItems,
    decimal TotalValue,
    string Status
);

/// <summary>
/// Invoice report data with summary and details
/// </summary>
public record InvoiceReportData(
    int TotalCount,
    decimal TotalRevenue,
    List<ChartDataPoint> ChartData,
    List<InvoiceReportItem> Items
);
