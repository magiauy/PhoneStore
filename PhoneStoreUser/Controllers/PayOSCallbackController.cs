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
    public async Task<IActionResult> HandleCallback([FromBody] dynamic data)
    {
        try
        {
            // TODO: Extract signature from headers and verify
            // For now, we'll trust the callback (should implement signature verification in production)

            string orderCode = data.orderCode?.ToString() ?? "";
            string status = data.status?.ToString() ?? "";

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
                _logger.LogWarning("Invoice not found: {InvoiceId}", invoiceId);
                return NotFound("Invoice not found");
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
    /// Return URL where PayOS redirects user after payment
    /// </summary>
    [HttpGet("return")]
    public IActionResult HandleReturn(
        [FromQuery] string? orderCode,
        [FromQuery] string? status,
        [FromQuery] bool? cancel)
    {
        if (string.IsNullOrEmpty(orderCode))
        {
            return Redirect("/order-error");
        }

        // Extract invoice ID from order code
        if (int.TryParse(orderCode, out int invoiceId))
        {
            var redirectUrl = $"/order-success?OrderId={invoiceId}";

            if (cancel == true)
            {
                redirectUrl += "&Cancel=true";
            }

            if (!string.IsNullOrEmpty(status))
            {
                redirectUrl += $"&Status={status}";
            }

            return Redirect(redirectUrl);
        }

        return Redirect("/order-error");
    }
}
