using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class OrderService : IOrderService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public OrderService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<int> CreateOrderAsync(int? personId, List<CartItem> items, CheckoutModel model, string paymentMethod)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(model);

        if (items.Count == 0)
        {
            throw new InvalidOperationException("Cart is empty");
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        int? resolvedPersonId = personId;

        if (resolvedPersonId is null)
        {
            var newPerson = new PersonEntity
            {
                FullName = model.FullName ?? string.Empty,
                Email = model.Email,
                Phone = model.Phone
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

                if (!string.IsNullOrWhiteSpace(model.Phone))
                {
                    personEntity.Phone = model.Phone;
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
        var invoice = new InvoiceEntity
        {
            PersonId = finalPersonId,
            CreatedBy = finalPersonId,
            InvoiceDate = DateTime.UtcNow,
            Status = string.Equals(paymentMethod, "PayOS", StringComparison.OrdinalIgnoreCase) ? "paid" : "unpaid",
            TotalAmount = totalAmount,
            DiscountAmount = 0,
            FinalAmount = totalAmount,
            PaymentMethod = paymentMethod,
            Note = model.Address
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
        await dbContext.SaveChangesAsync();

        return invoice.Id;
    }

    public async Task<List<InvoiceEntity>> GetOrdersAsync(int personId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var invoices = await dbContext.Invoices
            .Where(i => i.PersonId == personId)
            .OrderByDescending(i => i.InvoiceDate)
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
}
