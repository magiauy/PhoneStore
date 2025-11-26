using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class OrderService : IOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;
    private readonly IPayOSService _payOSService;

    public OrderService(
        IDbContextFactory<AppDbContext> dbContextFactory,
        IPayOSService payOSService)
    {
        _dbContextFactory = dbContextFactory;
        _payOSService = payOSService;
    }

    public async Task<int> CreateOrderAsync(int? personId, List<CartItem> items, CheckoutModel model, string paymentMethod, int? promotionCodeId = null, decimal discountAmount = 0)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(model);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? resolvedPersonId = personId;
        var phoneNumber = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim();

        if (resolvedPersonId is null && !string.IsNullOrWhiteSpace(phoneNumber))
        {
            var existingPerson = await dbContext.People.FirstOrDefaultAsync(p => p.Phone == phoneNumber);
            if (existingPerson is not null)
            {
                resolvedPersonId = existingPerson.Id;
            }
        }

        if (resolvedPersonId is null)
        {
            var newPerson = new PersonEntity
            {
                FullName = model.FullName ?? string.Empty,
                Email = model.Email,
                Phone = phoneNumber
            };

            dbContext.People.Add(newPerson);
            await dbContext.SaveChangesAsync();

            dbContext.Customers.Add(new CustomerEntity
            {
                PersonId = newPerson.Id,
                Address = model.Address
            });

            resolvedPersonId = newPerson.Id;
        }
        else
        {
            var personEntity = await dbContext.People.FirstOrDefaultAsync(p => p.Id == resolvedPersonId.Value);
            if (personEntity is not null)
            {
                if (!string.IsNullOrWhiteSpace(model.FullName))
                {
                    personEntity.FullName = model.FullName;
                }

                if (!string.IsNullOrWhiteSpace(model.Email))
                {
                    personEntity.Email = model.Email;
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    personEntity.Phone = phoneNumber;
                }
            }

            var customerEntity = await dbContext.Customers.FirstOrDefaultAsync(c => c.PersonId == resolvedPersonId.Value);
            if (customerEntity is not null)
            {
                if (!string.IsNullOrWhiteSpace(model.Address))
                {
                    customerEntity.Address = model.Address;
                }
            }
            else if (!string.IsNullOrWhiteSpace(model.Address))
            {
                dbContext.Customers.Add(new CustomerEntity
                {
                    PersonId = resolvedPersonId.Value,
                    Address = model.Address
                });
            }
        }

        var finalPersonId = resolvedPersonId ?? throw new InvalidOperationException("Unable to resolve customer information for the order.");

        var totalAmount = items.Sum(i => i.Product.Price * i.Quantity);
        var appliedDiscountAmount = ResolveDiscountAmount(totalAmount, discountAmount);
        var finalAmount = totalAmount - appliedDiscountAmount;

        var invoice = new InvoiceEntity
        {
            PersonId = finalPersonId,
            CreatedBy = finalPersonId,
            InvoiceDate = DateTime.UtcNow,
            Status = paymentMethod?.ToLowerInvariant() == "cod" ? "pending" : "unpaid",
            TotalAmount = totalAmount,
            DiscountAmount = appliedDiscountAmount,
            FinalAmount = finalAmount,
            PaymentMethod = paymentMethod,
            Note = model.Address,
            PromotionCodeId = promotionCodeId
        };

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync();

        var invoiceLines = items.Select(item => new InvoiceLineEntity
        {
            InvoiceId = invoice.Id,
            ProductId = item.Product.Id,
            Quantity = item.Quantity,
            UnitPrice = item.Product.Price,
            DiscountPct = 0,
            TotalPrice = item.Product.Price * item.Quantity
        }).ToList();

        dbContext.InvoiceLines.AddRange(invoiceLines);
        
        // Update promotion usage count if applicable
        if (promotionCodeId.HasValue)
        {
            var promoCode = await dbContext.PromotionCodes.FindAsync(promotionCodeId.Value);
            if (promoCode != null)
            {
                promoCode.UsedCount++;
            }
        }

        await dbContext.SaveChangesAsync();

        return invoice.Id;
    }

    public async Task<(int invoiceId, string? paymentUrl)> CreateOrderWithPaymentAsync(
        int? personId,
        List<CartItem> items,
        CheckoutModel model,
        int? promotionCodeId = null,
        decimal discountAmount = 0)
    {
        // Create the order with PayOS payment method
        var invoiceId = await CreateOrderAsync(personId, items, model, "bank", promotionCodeId, discountAmount);

        // Create PayOS payment link
        // PayOS requires orderCode to be an integer, so use invoiceId directly
        var orderCode = invoiceId;
        var totalAmount = items.Sum(i => i.Product.Price * i.Quantity);
        var appliedDiscountAmount = ResolveDiscountAmount(totalAmount, discountAmount);
        var finalAmount = totalAmount - appliedDiscountAmount;

        var description = $"Thanh toan don hang #{invoiceId}";

        var paymentResponse = await _payOSService.CreatePaymentLinkAsync(
            orderCode,
            finalAmount, // Use final amount after discount
            description,
            model.FullName ?? "Customer",
            model.Email ?? "",
            model.Phone ?? "",
            model.Address ?? "",
            items);

        if (paymentResponse?.Data?.CheckoutUrl != null)
        {
            return (invoiceId, paymentResponse.Data.CheckoutUrl);
        }

        return (invoiceId, null);
    }

    public async Task<List<InvoiceEntity>> GetOrdersAsync(int personId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoices = await dbContext.Invoices
            .Where(i => i.PersonId == personId)
            .OrderByDescending(i => i.InvoiceDate ?? DateTime.MinValue)
            .ToListAsync();

        if (!invoices.Any())
        {
            return invoices;
        }

        var invoiceIds = invoices.Select(i => i.Id).ToList();
        var invoiceLines = await dbContext.InvoiceLines
            .Where(line => invoiceIds.Contains(line.InvoiceId))
            .ToListAsync();

        var productIds = invoiceLines.Select(l => l.ProductId).Distinct().ToList();
        var products = await dbContext.Products
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var line in invoiceLines)
        {
            if (products.TryGetValue(line.ProductId, out var product))
            {
                line.Product = product;
            }
        }

        var linesLookup = invoiceLines.GroupBy(l => l.InvoiceId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var invoice in invoices)
        {
            if (linesLookup.TryGetValue(invoice.Id, out var lines))
            {
                invoice.Lines = lines;
            }
        }

        return invoices;
    }

    public async Task<InvoiceEntity?> GetOrderAsync(int invoiceId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoice = await dbContext.Invoices
            .Where(i => i.Id == invoiceId)
            .FirstOrDefaultAsync();

        if (invoice == null)
        {
            return null;
        }

        var invoiceLines = await dbContext.InvoiceLines
            .Where(line => line.InvoiceId == invoiceId)
            .ToListAsync();

        if (invoiceLines.Any())
        {
            var productIds = invoiceLines.Select(l => l.ProductId).Distinct().ToList();
            var products = await dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var line in invoiceLines)
            {
                if (products.TryGetValue(line.ProductId, out var product))
                {
                    line.Product = product;
                }
            }
        }

        invoice.Lines = invoiceLines;
        return invoice;
    }

    private static decimal ResolveDiscountAmount(decimal totalAmount, decimal discountAmount)
    {
        if (discountAmount <= 0)
        {
            return 0;
        }

        return discountAmount > totalAmount ? totalAmount : discountAmount;
    }
}
