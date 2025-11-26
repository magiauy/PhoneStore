using PhoneStoreUser.Models;

namespace PhoneStoreUser.Services;

public interface IPayOSService
{
    Task<PayOSPaymentResponse?> CreatePaymentLinkAsync(
        long orderCode,
        decimal amount,
        string description,
        string buyerName,
        string buyerEmail,
        string buyerPhone,
        string buyerAddress,
        IReadOnlyCollection<PhoneStoreUser.Components.Models.CartItem> items);
    bool VerifyPaymentCallback(string signature, string data);
    Task<bool> VerifyPaymentStatusAsync(long orderCode);
}
