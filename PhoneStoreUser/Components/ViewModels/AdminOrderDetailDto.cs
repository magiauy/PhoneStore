namespace PhoneStoreUser.Components.ViewModels;

public record AdminOrderDetailDto(
    int InvoiceId,
    string InvoiceCode,
    DateTime InvoiceDate,
    string CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string? CustomerAddress,
    string CreatedBy,
    string Status,
    string PaymentMethod,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal GrandTotal,
    string? PromoCode,
    List<OrderLineDto> Lines
);

public record OrderLineDto(
    int LineId,
    int ProductId,
    string ProductName,
    bool HasSerial,
    int Quantity,
    decimal UnitPrice,
    decimal DiscountPct,
    decimal TotalPrice,
    List<string> Serials
);
