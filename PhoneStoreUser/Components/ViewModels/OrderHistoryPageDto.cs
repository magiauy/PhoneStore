namespace PhoneStoreUser.Components.ViewModels;

public record OrderHistoryPageDto(
    List<OrderHistoryItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);

public record OrderHistoryItemDto(
    string Code,
    DateTime Date,
    string Customer,
    string Payment,
    string Status,
    decimal Total
);
