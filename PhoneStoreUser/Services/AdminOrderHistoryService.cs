using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminOrderHistoryService : IAdminOrderHistoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminOrderHistoryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<OrderHistoryPageDto> GetOrderHistoryAsync(int page, int pageSize, string? search, string? statusFilter, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.Invoices.AsQueryable();

        // Apply date filter
        if (startDate.HasValue)
        {
            query = query.Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            // Include the entire end day
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value <= endOfDay);
        }

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchFilterPersonIds = await dbContext.People
                .Where(p => p.FullName.Contains(search) || (p.Phone != null && p.Phone.Contains(search)))
                .Select(p => p.Id)
                .ToListAsync();

            query = query.Where(i =>
                i.Id.ToString().Contains(search) ||
                (i.PersonId.HasValue && searchFilterPersonIds.Contains(i.PersonId.Value)));
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "all")
        {
            query = query.Where(i => i.Status == statusFilter);
        }

        // Get total count for pagination
        var totalCount = await query.CountAsync();

        // Apply pagination
        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate ?? DateTime.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Load person info
        var personIds = invoices.Where(i => i.PersonId.HasValue).Select(i => i.PersonId!.Value).Distinct().ToList();
        var persons = await dbContext.People
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var items = invoices.Select(inv =>
        {
            var person = inv.PersonId.HasValue && persons.TryGetValue(inv.PersonId.Value, out var p) ? p : null;
            return new OrderHistoryItemDto(
                Code: $"INV-{inv.Id:D6}",
                Date: inv.InvoiceDate,
                Customer: person?.FullName ?? "Khách vãng lai",
                Payment: inv.PaymentMethod,
                Status: inv.Status,
                Total: inv.FinalAmount
            );
        }).ToList();

        return new OrderHistoryPageDto(
            Items: items,
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate, DateTime? endDate)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Default to last 30 days if not specified
        var statsStart = startDate ?? DateTime.UtcNow.AddDays(-30);
        var statsEnd = endDate ?? DateTime.UtcNow;

        var orders = await dbContext.Invoices
            .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value >= statsStart && i.InvoiceDate.Value <= statsEnd)
            .ToListAsync();

        var totalRevenue = orders
            .Where(i => i.Status == "completed" || i.Status == "delivering")
            .Sum(i => i.FinalAmount);

        var totalOrders = orders.Count;

        var completedOrders = orders.Count(i => i.Status == "completed");
        var completionRate = totalOrders > 0 ? (decimal)completedOrders / totalOrders * 100 : 0;

        var cancelledOrRefunded = orders.Count(i => i.Status == "cancelled" || i.Status == "refunded");

        return new OrderStatisticsDto(
            TotalRevenue: totalRevenue,
            TotalOrders: totalOrders,
            CompletionRate: Math.Round(completionRate, 2),
            CancelledOrRefundedCount: cancelledOrRefunded
        );
    }
}
