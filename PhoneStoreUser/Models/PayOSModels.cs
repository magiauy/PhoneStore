using System.Text.Json.Serialization;

namespace PhoneStoreUser.Models;

public class PayOSConfig
{
    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api-merchant.payos.vn";
    public string ReturnUrl { get; set; } = string.Empty;
    public string CallbackUrl { get; set; } = string.Empty;
}

public class PayOSPaymentRequest
{
    public long OrderCode { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}

public class PayOSPaymentResponse
{
    public string Code { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public PayOSPaymentData? Data { get; set; }
}

public class PayOSPaymentData
{
    public string Bin { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    [JsonIgnore]
    public long? OrderCodeValue => long.TryParse(OrderCode, out var value) ? value : null;
    public string Currency { get; set; } = "VND";
    public string PaymentLinkId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
}
