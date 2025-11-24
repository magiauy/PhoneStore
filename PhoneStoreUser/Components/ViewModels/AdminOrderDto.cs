namespace PhoneStoreUser.Components.ViewModels;

public record AdminOrderDto(
    int InvoiceId,
    string InvoiceCode,
    DateTime? InvoiceDate,
    string CustomerName,
    string? CustomerPhone,
    string Status,
    string PaymentMethod,
    decimal Total,
    string? PromoCode
);
