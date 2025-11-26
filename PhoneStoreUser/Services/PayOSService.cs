using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using PhoneStoreUser.Models;

namespace PhoneStoreUser.Services;

public class PayOSService : IPayOSService
{
    private readonly PayOSConfig _config;
    private readonly HttpClient _httpClient;
    private readonly ILogger<PayOSService> _logger;
    private static readonly JsonSerializerOptions PayOsSerializerOptions = CreateSerializerOptions();

    public PayOSService(
        IOptions<PayOSConfig> config,
        HttpClient httpClient,
        ILogger<PayOSService> logger)
    {
        _config = config.Value;
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_config.ApiUrl);
    }

    public async Task<PayOSPaymentResponse?> CreatePaymentLinkAsync(
        long orderCode,
        decimal amount,
        string description,
        string buyerName,
        string buyerEmail,
        string buyerPhone,
        string buyerAddress,
        IReadOnlyCollection<PhoneStoreUser.Components.Models.CartItem> items)
    {
        try
        {
            // Use detailed line items to build both the PayOS payload and the total amount
            var itemPayloads = items.Select(i => new
            {
                name = i.Product.Name,
                quantity = i.Quantity,
                price = (int)i.Product.Price
            }).ToList();

            var amountInt = itemPayloads.Sum(i => i.price * i.quantity);

            var expiredAt = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

            // Build signature data string according to PayOS format
            // Must use integer amount in signature, sorted alphabetically
            var signatureData = $"amount={amountInt}&cancelUrl={_config.ReturnUrl}&description={description}&orderCode={orderCode}&returnUrl={_config.ReturnUrl}";
            var signature = ComputeHmacSha256(signatureData, _config.ChecksumKey);

            var request = new
            {
                orderCode = orderCode,
                amount = amountInt,
                description = description,
                expiredAt = expiredAt,
                buyerName = buyerName,
                buyerEmail = buyerEmail,
                buyerPhone = buyerPhone,
                buyerAddress = buyerAddress,
                items = itemPayloads,
                cancelUrl = _config.ReturnUrl,
                returnUrl = _config.ReturnUrl,
                signature = signature
            };

            var jsonContent = JsonSerializer.Serialize(request);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/v2/payment-requests")
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            httpRequest.Headers.Add("x-client-id", _config.ClientId);
            httpRequest.Headers.Add("x-api-key", _config.ApiKey);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayOS API error: {Content}", responseContent);
                return null;
            }

            // Log the actual response for debugging
            _logger.LogInformation("PayOS API response: {Response}", responseContent);

            var paymentResponse = JsonSerializer.Deserialize<PayOSPaymentResponse>(
                responseContent,
                PayOsSerializerOptions);

            // Log deserialized response
            _logger.LogInformation("Deserialized response - Code: {Code}, CheckoutUrl: {Url}",
                paymentResponse?.Code,
                paymentResponse?.Data?.CheckoutUrl);

            return paymentResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayOS payment link");
            return null;
        }
    }

    public bool VerifyPaymentCallback(string signature, string data)
    {
        try
        {
            var computedSignature = ComputeHmacSha256(data, _config.ChecksumKey);
            return signature.Equals(computedSignature, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment callback");
            return false;
        }
    }

    public async Task<bool> VerifyPaymentStatusAsync(long orderCode)
    {
        try
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            var signature = GenerateSignature($"orderCode={orderCode}", timestamp);

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"/v2/payment-requests/{orderCode}");
            httpRequest.Headers.Add("x-client-id", _config.ClientId);
            httpRequest.Headers.Add("x-api-key", _config.ApiKey);
            httpRequest.Headers.Add("x-timestamp", timestamp);
            httpRequest.Headers.Add("x-signature", signature);

            var response = await _httpClient.SendAsync(httpRequest);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var paymentResponse = JsonSerializer.Deserialize<PayOSPaymentResponse>(
                responseContent,
                PayOsSerializerOptions);

            return paymentResponse?.Data?.Status?.ToLower() == "paid";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment status");
            return false;
        }
    }

    private string GenerateSignature(string data, string timestamp)
    {
        var message = $"{timestamp}.{data}";
        return ComputeHmacSha256(message, _config.ChecksumKey);
    }

    private static string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new FlexibleStringJsonConverter());
        return options;
    }

    private sealed class FlexibleStringJsonConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString() ?? string.Empty,
                JsonTokenType.Number => ReadNumber(ref reader),
                JsonTokenType.True => bool.TrueString,
                JsonTokenType.False => bool.FalseString,
                JsonTokenType.Null => string.Empty,
                _ => ReadComplexValue(ref reader)
            };
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }

        private static string ReadNumber(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt64(out var longValue))
            {
                return longValue.ToString(CultureInfo.InvariantCulture);
            }

            if (reader.TryGetDouble(out var doubleValue))
            {
                return doubleValue.ToString(CultureInfo.InvariantCulture);
            }

            return reader.GetDecimal().ToString(CultureInfo.InvariantCulture);
        }

        private static string ReadComplexValue(ref Utf8JsonReader reader)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            return document.RootElement.GetRawText();
        }
    }
}
