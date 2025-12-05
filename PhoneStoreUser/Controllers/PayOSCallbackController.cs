using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;
using PhoneStoreUser.Services;

namespace PhoneStoreUser.Controllers;

[ApiController]
[Route("api/payos")]
public class PayOSCallbackController : ControllerBase
{
    private readonly IPayOSService _payOSService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly ILogger<PayOSCallbackController> _logger;

    public PayOSCallbackController(
        IPayOSService payOSService,
        IDbContextFactory<AppDbContext> dbContextFactory,
        ILogger<PayOSCallbackController> logger)
    {
        _payOSService = payOSService;
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <summary>
    /// PayOS webhook callback endpoint
    /// Called by PayOS when payment status changes
    /// </summary>
    [HttpPost("callback")]
    public async Task<IActionResult> HandleCallback([FromBody] JsonElement payload)
    {
        try
        {
            // TODO: Extract signature from headers and verify
            // For now, we'll trust the callback (should implement signature verification in production)

            var orderCode = ResolveValue(payload, "orderCode")
                ?? (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("data", out var dataElement)
                    ? ResolveValue(dataElement, "orderCode")
                    : null)
                ?? string.Empty;

            var status = ResolveValue(payload, "status")
                ?? (payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("data", out var dataElement2)
                    ? ResolveValue(dataElement2, "status")
                    : null)
                ?? string.Empty;

            if (string.IsNullOrEmpty(orderCode))
            {
                return BadRequest("Invalid order code");
            }

            // Extract invoice ID from order code (PayOS requires integer orderCode, so we use invoiceId directly)
            if (!int.TryParse(orderCode, out int invoiceId))
            {
                _logger.LogWarning("Invalid order code format: {OrderCode}", orderCode);
                return BadRequest("Invalid order code format");
            }

            // Update invoice status based on payment status
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var invoice = await dbContext.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                _logger.LogInformation("Received PayOS callback for non-existent invoice {InvoiceId}", invoiceId);
                return Ok(new
                {
                    success = false,
                    message = "Invoice not found"
                });
            }

            // Update status based on PayOS payment result
            invoice.Status = status.ToLower() switch
            {
                "paid" or "success" => "paid",
                "cancelled" or "canceled" => "cancelled",
                _ => invoice.Status // Keep current status for unknown states
            };

            await dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated invoice {InvoiceId} status to {Status}", invoiceId, invoice.Status);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PayOS callback");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Simple GET endpoint so PayOS can ping the webhook URL to verify reachability.
    /// </summary>
    [HttpGet("callback")]
    public IActionResult CallbackHealthCheck()
    {
        return Ok(new { success = true });
    }

    private static string? ResolveValue(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!element.TryGetProperty(property, out var valueElement))
        {
            return null;
        }

        return valueElement.ValueKind switch
        {
            JsonValueKind.String => valueElement.GetString(),
            JsonValueKind.Number => valueElement.TryGetInt64(out var longValue)
                ? longValue.ToString(CultureInfo.InvariantCulture)
                : valueElement.GetRawText(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => valueElement.GetRawText()
        };
    }

    /// <summary>
    /// Return URL where PayOS redirects user after payment
    /// </summary>
    [HttpGet("return")]
    public IActionResult HandleReturn(
        [FromQuery] string? orderCode,
        [FromQuery] string? status,
        [FromQuery] bool? cancel)
    {
        if (string.IsNullOrEmpty(orderCode) || !int.TryParse(orderCode, out var invoiceId))
        {
            return Redirect("/order-error");
        }

        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateInvoiceStatusFromReturnAsync(invoiceId, status, cancel);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update invoice {InvoiceId} status from return URL", invoiceId);
            }
        });

        // Use lowercase query params to match Blazor's SupplyParameterFromQuery (case-insensitive by default)
        var queryParams = new List<string> { $"OrderId={invoiceId}" };
        
        if (cancel == true)
        {
            queryParams.Add("Cancel=true");
        }

        if (!string.IsNullOrEmpty(status))
        {
            queryParams.Add($"Status={Uri.EscapeDataString(status)}");
        }

        var redirectUrl = "/order-success?" + string.Join("&", queryParams);

        return Redirect(redirectUrl);
    }

    private async Task UpdateInvoiceStatusFromReturnAsync(int invoiceId, string? status, bool? cancel)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var invoice = await dbContext.Invoices.FindAsync(invoiceId);

        if (invoice == null)
        {
            _logger.LogInformation("Return URL hit for non-existent invoice {InvoiceId}", invoiceId);
            return;
        }

        var normalizedStatus = cancel == true
            ? "cancelled"
            : status?.ToLowerInvariant();

        if (string.IsNullOrEmpty(normalizedStatus))
        {
            // If no explicit status, ask PayOS to confirm
            var isPaid = await _payOSService.VerifyPaymentStatusAsync(invoiceId);
            normalizedStatus = isPaid ? "paid" : null;
        }
        else
        {
            normalizedStatus = normalizedStatus switch
            {
                "paid" or "success" => "paid",
                "cancelled" or "canceled" => "cancelled",
                _ => normalizedStatus
            };
        }

        if (!string.IsNullOrEmpty(normalizedStatus) && !string.Equals(invoice.Status, normalizedStatus, StringComparison.OrdinalIgnoreCase))
        {
            invoice.Status = normalizedStatus;
            await dbContext.SaveChangesAsync();
            _logger.LogInformation("Updated invoice {InvoiceId} status to {Status} from return URL", invoiceId, invoice.Status);
        }
    }
}
