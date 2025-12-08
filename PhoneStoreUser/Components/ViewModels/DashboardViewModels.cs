namespace PhoneStoreUser.Components.ViewModels;

/// <summary>
/// Monthly statistics for the dashboard
/// </summary>
public record MonthlyStatistics(
    decimal TotalRevenue,
    int TotalOrders,
    int NewCustomers,
    decimal RevenueGrowthPercent,
    int OrderGrowthPercent
);

/// <summary>
/// Top selling product information
/// </summary>
public record TopProductDto(
    int ProductId,
    string ProductName,
    int QuantitySold,
    decimal TotalRevenue
);

/// <summary>
/// Top customer by purchase value
/// </summary>
public record TopCustomerDto(
    int CustomerId,
    string CustomerName,
    int OrderCount,
    decimal TotalPurchaseValue
);
