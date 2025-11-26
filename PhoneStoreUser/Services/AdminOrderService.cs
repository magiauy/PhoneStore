using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;
using System.Linq;

namespace PhoneStoreUser.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IInventoryService _inventoryService;

    public AdminOrderService(IDbContextFactory<AppDbContext> dbContextFactory, IInventoryService inventoryService)
    {
        _dbContextFactory = dbContextFactory;
        _inventoryService = inventoryService;
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
            .OrderByDescending(i => i.InvoiceDate ?? DateTime.MinValue)
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
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
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

            if (newStatus == "completed")
            {
                await AdjustInventoryForInvoiceAsync(dbContext, invoice.Id);
            }

            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
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
        // COD orders (cod/cash): pending → delivering → completed

        var normalizedStatus = currentStatus?.ToLowerInvariant() ?? string.Empty;
        var normalizedPaymentMethod = paymentMethod?.ToLowerInvariant() ?? string.Empty;

        return (normalizedStatus, normalizedPaymentMethod) switch
        {
            ("unpaid", "bank") => new List<string> { "paid", "cancelled" },
            ("paid", "bank") => new List<string> { "delivering", "cancelled" },
            ("pending", "cod") => new List<string> { "delivering", "cancelled" },
            ("delivering", _) => new List<string> { "completed", "refunded" },
            _ => new List<string>()
        };
    }
    public async Task<SerialValidationResult> ValidateSerialAsync(string serial, int productId)
    {
        if (string.IsNullOrWhiteSpace(serial))
        {
            return new SerialValidationResult(false, "Serial cannot be empty");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // 1. Check if serial exists
        var productSerial = await dbContext.ProductSerials
            .Include(ps => ps.Product)
            .FirstOrDefaultAsync(ps => ps.SerialNumber == serial || ps.Imei1 == serial || ps.Imei2 == serial);

        if (productSerial == null)
        {
            return new SerialValidationResult(false, "Serial không tồn tại trong hệ thống");
        }

        // 2. Check if serial belongs to the correct product
        if (productSerial.ProductId != productId)
        {
            return new SerialValidationResult(false, $"Serial này thuộc về sản phẩm khác: {productSerial.Product?.Name ?? "Unknown"}");
        }

        // 3. Check status
        if (productSerial.Status != "in_stock")
        {
            return new SerialValidationResult(false, $"Serial không khả dụng (Trạng thái: {productSerial.Status})");
        }

        return new SerialValidationResult(true, "Hợp lệ");
    }

    public async Task CreateCompletedOrderAsync(CreateOrderDto dto)
    {
        if (dto.Lines == null || !dto.Lines.Any())
        {
            throw new InvalidOperationException("Order must have at least one item");
        }

        var availabilityLookup = await _inventoryService
            .GetAvailabilityForProductsAsync(dto.Lines.Select(l => l.ProductId));

        foreach (var line in dto.Lines.Where(l => !l.IsSerialTracked))
        {
            var available = availabilityLookup.TryGetValue(line.ProductId, out var snapshot)
                ? snapshot.AvailableQuantity
                : 0;

            if (line.Quantity > available)
            {
                throw new InvalidOperationException($"S?n ph?m {line.ProductName} ch? c�n {available} s?n ph?m trong kho.");
            }
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        using var transaction = await dbContext.Database.BeginTransactionAsync();

        try
        {
            var normalizedPhone = dto.CustomerPhone?.Trim();
            dto.CustomerPhone = normalizedPhone;
            dto.CustomerName = dto.CustomerName?.Trim();
            var customerId = dto.CustomerId;

            if (customerId == null && !string.IsNullOrWhiteSpace(normalizedPhone))
            {
                var existingCustomer = await dbContext.Persons
                    .FirstOrDefaultAsync(p => p.Phone == normalizedPhone && p.PersonType == "CUSTOMER");

                if (existingCustomer != null)
                {
                    customerId = existingCustomer.Id;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(dto.CustomerName))
                    {
                        throw new InvalidOperationException("Vui l�ng nh?p t�n kh�ch h�ng.");
                    }

                    var newPerson = new PersonEntity
                    {
                        FullName = dto.CustomerName!,
                        Phone = normalizedPhone,
                        Email = null,
                        PersonType = "CUSTOMER",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true,
                        Code = await GenerateCustomerCode(dbContext)
                    };

                    dbContext.Persons.Add(newPerson);
                    await dbContext.SaveChangesAsync();

                    var newCustomer = new CustomerEntity
                    {
                        PersonId = newPerson.Id,
                        Address = dto.Note,
                        LastOrderDate = DateTime.UtcNow,
                        TotalSpend = 0
                    };

                    dbContext.Customers.Add(newCustomer);
                    await dbContext.SaveChangesAsync();

                    customerId = newPerson.Id;
                }
            }

            // 1. Create Invoice
            var invoice = new InvoiceEntity
            {
                PersonId = customerId,
                CreatedBy = 1, // TODO: Get current user ID. For now hardcode to 1 (admin) or need to inject UserSession
                InvoiceDate = DateTime.UtcNow,
                Status = "completed",
                PaymentMethod = dto.PaymentMethod,
                Note = dto.Note,
                TotalAmount = 0,
                DiscountAmount = 0,
                FinalAmount = 0
            };

            dbContext.Invoices.Add(invoice);
            await dbContext.SaveChangesAsync();

            decimal totalAmount = 0;

            // 2. Create Invoice Lines
            foreach (var lineDto in dto.Lines)
            {
                var lineTotal = lineDto.Quantity * lineDto.UnitPrice;
                totalAmount += lineTotal;

                var line = new InvoiceLineEntity
                {
                    InvoiceId = invoice.Id,
                    ProductId = lineDto.ProductId,
                    Quantity = lineDto.Quantity,
                    UnitPrice = lineDto.UnitPrice,
                    DiscountPct = 0,
                    TotalPrice = lineTotal
                };

                dbContext.InvoiceLines.Add(line);
                await dbContext.SaveChangesAsync();

                // 3. Handle Serials
                if (lineDto.IsSerialTracked)
                {
                    if (lineDto.SerialNumbers.Count != lineDto.Quantity)
                    {
                        throw new InvalidOperationException($"Product {lineDto.ProductName} requires {lineDto.Quantity} serials, but {lineDto.SerialNumbers.Count} provided.");
                    }

                    foreach (var serial in lineDto.SerialNumbers)
                    {
                        // Validate and Reserve/Sell serial
                        var productSerial = await dbContext.ProductSerials
                            .FirstOrDefaultAsync(ps => ps.SerialNumber == serial || ps.Imei1 == serial || ps.Imei2 == serial);

                        if (productSerial == null)
                        {
                            throw new InvalidOperationException($"Serial {serial} not found for product {lineDto.ProductName}");
                        }

                        if (productSerial.ProductId != lineDto.ProductId)
                        {
                            throw new InvalidOperationException($"Serial {serial} does not belong to product {lineDto.ProductName}");
                        }

                        if (productSerial.Status != "in_stock")
                        {
                            throw new InvalidOperationException($"Serial {serial} is not available (Status: {productSerial.Status})");
                        }

                        // Update status to sold
                        productSerial.Status = "sold";
                        
                        // Link to invoice line
                        var lineSerial = new InvoiceLineSerialEntity
                        {
                            InvoiceLineId = line.Id,
                            ProductSerialId = productSerial.Id
                        };
                        dbContext.InvoiceLineSerials.Add(lineSerial);
                    }
                }
            }

            // 4. Update Invoice Totals
            invoice.TotalAmount = totalAmount;
            invoice.FinalAmount = totalAmount; // No discount logic for now
            
            await dbContext.SaveChangesAsync();
            await AdjustInventoryForInvoiceAsync(dbContext, invoice.Id);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private static async Task AdjustInventoryForInvoiceAsync(AppDbContext dbContext, int invoiceId)
    {
        var lines = await dbContext.InvoiceLines
            .Where(l => l.InvoiceId == invoiceId)
            .Include(l => l.Product)
            .Include(l => l.LineSerials)
                .ThenInclude(ls => ls.ProductSerial)
            .ToListAsync();

        foreach (var line in lines)
        {
            if (line.Product?.IsSerialTracked == true)
            {
                foreach (var lineSerial in line.LineSerials)
                {
                    var serial = lineSerial.ProductSerial ?? await dbContext.ProductSerials.FindAsync(lineSerial.ProductSerialId);
                    if (serial == null)
                    {
                        continue;
                    }

                    if (!string.Equals(serial.Status, "sold", StringComparison.OrdinalIgnoreCase))
                    {
                        serial.Status = "sold";
                    }
                }
            }
            else
            {
                await DeductBatchQuantityAsync(dbContext, line.ProductId, line.Quantity);
            }
        }
    }

    private static async Task DeductBatchQuantityAsync(AppDbContext dbContext, int productId, int quantity)
    {
        if (quantity <= 0)
        {
            return;
        }

        var remaining = quantity;
        var batchProducts = await dbContext.BatchProducts
            .Where(bp => bp.ProductId == productId && bp.Quantity > 0)
            .OrderBy(bp => bp.BatchId)
            .ThenBy(bp => bp.Id)
            .ToListAsync();

        foreach (var batchProduct in batchProducts)
        {
            if (remaining <= 0)
            {
                break;
            }

            var deduction = Math.Min(batchProduct.Quantity, remaining);
            batchProduct.Quantity -= deduction;
            remaining -= deduction;
        }

        if (remaining > 0)
        {
            throw new InvalidOperationException($"Kho kh�ng d? cho s?n ph?m ID {productId}. Thieu {remaining} s?n ph?m.");
        }
    }

    public async Task<CustomerLookupResult?> FindCustomerByPhoneAsync(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var normalizedPhone = phone.Trim();
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var person = await dbContext.Persons
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonType == "CUSTOMER" && p.Phone == normalizedPhone);

        if (person == null)
        {
            return null;
        }

        return new CustomerLookupResult(person.Id, person.FullName, person.Phone ?? normalizedPhone);
    }

    private static async Task<string> GenerateCustomerCode(AppDbContext context)
    {
        var lastPerson = await context.Persons
            .Where(p => p.PersonType == "CUSTOMER" && p.Code != null && p.Code.StartsWith("KH"))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync();

        var nextNum = 1;
        if (lastPerson?.Code is string code && code.Length > 2 && int.TryParse(code.Substring(2), out var currentNum))
        {
            nextNum = currentNum + 1;
        }

        return $"KH{nextNum:D6}";
    }
}
