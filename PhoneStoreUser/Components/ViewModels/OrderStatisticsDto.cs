namespace PhoneStoreUser.Components.ViewModels;

public record OrderStatisticsDto(
    decimal TotalRevenue,
    int TotalOrders,
    decimal CompletionRate,
    int CancelledOrRefundedCount
);
