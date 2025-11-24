using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminOrderService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<List<AdminOrderDto>> GetPendingOrdersAsync(string? statusFilter = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.Invoices
            .Include(i => i.Lines)
            .AsQueryable();

        // Filter by status if provided
        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            query = query.Where(i => i.Status == statusFilter);
        }
        else
        {
            // Default: show orders that need processing (not completed, not cancelled, not refunded)
            query = query.Where(i => i.Status != "completed" && i.Status != "cancelled" && i.Status != "refunded");
        }

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        if (!invoices.Any())
        {
            return new List<AdminOrderDto>();
        }

        // Load persons for customer info
        var personIds = invoices.Where(i => i.PersonId.HasValue).Select(i => i.PersonId!.Value).Distinct().ToList();
        var persons = await dbContext.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        return invoices.Select(inv =>
        {
            var person = inv.PersonId.HasValue && persons.TryGetValue(inv.PersonId.Value, out var p) ? p : null;
            return new AdminOrderDto(
                InvoiceId: inv.Id,
                InvoiceCode: $"INV-{inv.Id:D6}",
                InvoiceDate: inv.InvoiceDate,
                CustomerName: person?.FullName ?? "Khách vãng lai",
                CustomerPhone: person?.Phone,
                Status: inv.Status,
                PaymentMethod: inv.PaymentMethod,
                Total: inv.FinalAmount,
                PromoCode: inv.PromotionCodeId.HasValue ? $"PROMO-{inv.PromotionCodeId}" : null
            );
        }).ToList();
    }

    public async Task<AdminOrderDetailDto?> GetOrderDetailAsync(int invoiceId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoice = await dbContext.Invoices
            .Include(i => i.Lines)
                .ThenInclude(l => l.Product)
            .Include(i => i.Lines)
                .ThenInclude(l => l.LineSerials)
                    .ThenInclude(ls => ls.ProductSerial)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            return null;
        }

        // Load customer and creator info
        PersonEntity? customer = null;
        PersonEntity? creator = null;
        CustomerEntity? customerDetails = null;

        if (invoice.PersonId.HasValue)
        {
            customer = await dbContext.People.FirstOrDefaultAsync(p => p.Id == invoice.PersonId.Value);
            customerDetails = await dbContext.Customers.FirstOrDefaultAsync(c => c.PersonId == invoice.PersonId.Value);
        }

        creator = await dbContext.People.FirstOrDefaultAsync(p => p.Id == invoice.CreatedBy);

        var lines = invoice.Lines.Select(line =>
        {
            var serials = line.LineSerials
                .Select(ls => ls.ProductSerial?.SerialNumber ?? ls.ProductSerial?.Imei1 ?? "N/A")
                .ToList();

            return new OrderLineDto(
                LineId: line.Id,
                ProductId: line.ProductId,
                ProductName: line.Product?.Name ?? "Unknown Product",
                HasSerial: line.Product?.IsSerialTracked ?? false,
                Quantity: line.Quantity,
                UnitPrice: line.UnitPrice,
                DiscountPct: line.DiscountPct,
                TotalPrice: line.TotalPrice,
                Serials: serials
            );
        }).ToList();

        return new AdminOrderDetailDto(
            InvoiceId: invoice.Id,
            InvoiceCode: $"INV-{invoice.Id:D6}",
            InvoiceDate: invoice.InvoiceDate,
            CustomerName: customer?.FullName ?? "Khách vãng lai",
            CustomerPhone: customer?.Phone,
            CustomerEmail: customer?.Email,
            CustomerAddress: customerDetails?.Address ?? invoice.Note,
            CreatedBy: creator?.FullName ?? "System",
            Status: invoice.Status,
            PaymentMethod: invoice.PaymentMethod,
            Subtotal: invoice.TotalAmount,
            DiscountAmount: invoice.DiscountAmount,
            GrandTotal: invoice.FinalAmount,
            PromoCode: invoice.PromotionCodeId.HasValue ? $"PROMO-{invoice.PromotionCodeId}" : null,
            Lines: lines
        );
    }

    public async Task UpdateOrderStatusAsync(int invoiceId, string newStatus)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null)
        {
            throw new InvalidOperationException($"Invoice {invoiceId} not found");
        }

        // Validate status transitions
        var validTransitions = GetValidStatusTransitions(invoice.Status, invoice.PaymentMethod);
        if (!validTransitions.Contains(newStatus))
        {
            throw new InvalidOperationException($"Cannot transition from {invoice.Status} to {newStatus}");
        }

        invoice.Status = newStatus;
        await dbContext.SaveChangesAsync();
    }

    public async Task CancelOrderAsync(int invoiceId, string reason)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (invoice == null)
        {
            throw new InvalidOperationException($"Invoice {invoiceId} not found");
        }

        if (invoice.Status == "completed" || invoice.Status == "cancelled")
        {
            throw new InvalidOperationException($"Cannot cancel order with status {invoice.Status}");
        }

        invoice.Status = "cancelled";
        invoice.Note = string.IsNullOrWhiteSpace(invoice.Note) ? $"Cancelled: {reason}" : $"{invoice.Note} | Cancelled: {reason}";
        await dbContext.SaveChangesAsync();
    }

    public async Task AddSerialToOrderLineAsync(int invoiceLineId, string? serialNumber, string? imei1, string? imei2)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoiceLine = await dbContext.InvoiceLines
            .Include(l => l.Product)
            .FirstOrDefaultAsync(l => l.Id == invoiceLineId);

        if (invoiceLine == null)
        {
            throw new InvalidOperationException($"Invoice line {invoiceLineId} not found");
        }

        if (!invoiceLine.Product?.IsSerialTracked ?? true)
        {
            throw new InvalidOperationException("This product does not track serials");
        }

        // Check for duplicate serial/IMEI
        if (!string.IsNullOrWhiteSpace(serialNumber))
        {
            var duplicateSerial = await dbContext.ProductSerials
                .AnyAsync(ps => ps.SerialNumber == serialNumber);
            if (duplicateSerial)
            {
                throw new InvalidOperationException($"Serial {serialNumber} already exists");
            }
        }

        if (!string.IsNullOrWhiteSpace(imei1))
        {
            var duplicateImei = await dbContext.ProductSerials
                .AnyAsync(ps => ps.Imei1 == imei1);
            if (duplicateImei)
            {
                throw new InvalidOperationException($"IMEI {imei1} already exists");
            }
        }

        // Create product serial
        var productSerial = new ProductSerialEntity
        {
            ProductId = invoiceLine.ProductId,
            SerialNumber = serialNumber,
            Imei1 = imei1,
            Imei2 = imei2,
            Status = "reserved"
        };

        dbContext.ProductSerials.Add(productSerial);
        await dbContext.SaveChangesAsync();

        // Link to invoice line
        var invoiceLineSerial = new InvoiceLineSerialEntity
        {
            InvoiceLineId = invoiceLineId,
            ProductSerialId = productSerial.Id
        };

        dbContext.InvoiceLineSerials.Add(invoiceLineSerial);
        await dbContext.SaveChangesAsync();

        // Update serial status to sold when order is delivering/completed
        var invoice = await dbContext.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceLine.InvoiceId);
        if (invoice?.Status == "delivering" || invoice?.Status == "completed")
        {
            productSerial.Status = "sold";
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<List<string>> GetOrderLineSerialsAsync(int invoiceLineId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var serials = await dbContext.InvoiceLineSerials
            .Where(ils => ils.InvoiceLineId == invoiceLineId)
            .Include(ils => ils.ProductSerial)
            .Select(ils => ils.ProductSerial!.SerialNumber ?? ils.ProductSerial!.Imei1 ?? "N/A")
            .ToListAsync();

        return serials;
    }

    private List<string> GetValidStatusTransitions(string currentStatus, string paymentMethod)
    {
        // Online orders (bank): unpaid → paid → delivering → completed
        // COD orders (cash): pending → delivering → completed

        return (currentStatus, paymentMethod) switch
        {
            ("unpaid", "bank") => new List<string> { "paid", "cancelled" },
            ("paid", "bank") => new List<string> { "delivering", "cancelled" },
            ("pending", "cash") => new List<string> { "delivering", "cancelled" },
            ("delivering", _) => new List<string> { "completed", "refunded" },
            _ => new List<string>()
        };
    }
}
