using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service implementation for dashboard statistics and analytics
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DashboardService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Get monthly statistics for the specified year and month
    /// </summary>
    public async Task<MonthlyStatistics> GetMonthlyStatisticsAsync(int year, int month)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Calculate date range for the selected month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Calculate date range for the previous month (for growth comparison)
        var prevStartDate = startDate.AddMonths(-1);
        var prevEndDate = startDate;

        // Get current month invoices (only completed/paid invoices count as revenue)
        var currentMonthInvoices = await dbContext.Invoices
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate)
            .Where(i => i.Status == "completed" || i.Status == "paid" || i.Status == "delivering")
            .ToListAsync();

        var totalRevenue = currentMonthInvoices.Sum(i => i.FinalAmount);
        var totalOrders = currentMonthInvoices.Count;

        // Get new customers for the selected month
        var newCustomers = await dbContext.Persons
            .Where(p => p.PersonType == "CUSTOMER")
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt < endDate)
            .CountAsync();

        // Get previous month data for growth calculation
        var prevMonthInvoices = await dbContext.Invoices
            .Where(i => i.InvoiceDate >= prevStartDate && i.InvoiceDate < prevEndDate)
            .Where(i => i.Status == "completed" || i.Status == "paid" || i.Status == "delivering")
            .ToListAsync();

        var prevRevenue = prevMonthInvoices.Sum(i => i.FinalAmount);
        var prevOrders = prevMonthInvoices.Count;

        // Calculate growth percentages
        var revenueGrowthPercent = prevRevenue > 0
            ? Math.Round((totalRevenue - prevRevenue) / prevRevenue * 100, 2)
            : (totalRevenue > 0 ? 100m : 0m);

        var orderGrowthPercent = prevOrders > 0
            ? (int)Math.Round((double)(totalOrders - prevOrders) / prevOrders * 100)
            : (totalOrders > 0 ? 100 : 0);

        return new MonthlyStatistics(
            TotalRevenue: totalRevenue,
            TotalOrders: totalOrders,
            NewCustomers: newCustomers,
            RevenueGrowthPercent: revenueGrowthPercent,
            OrderGrowthPercent: orderGrowthPercent
        );
    }


    /// <summary>
    /// Get top selling products for the specified year and month
    /// </summary>
    public async Task<List<TopProductDto>> GetTopSellingProductsAsync(int year, int month, int count = 10)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Calculate date range for the selected month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Get invoice lines from completed/paid invoices in the selected month
        var topProducts = await dbContext.InvoiceLines
            .Include(il => il.Invoice)
            .Include(il => il.Product)
            .Where(il => il.Invoice != null && il.Invoice.InvoiceDate >= startDate && il.Invoice.InvoiceDate < endDate)
            .Where(il => il.Invoice != null && (il.Invoice.Status == "completed" || il.Invoice.Status == "paid" || il.Invoice.Status == "delivering"))
            .GroupBy(il => new { il.ProductId, ProductName = il.Product != null ? il.Product.Name : "Unknown" })
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.ProductName,
                QuantitySold = g.Sum(il => il.Quantity),
                TotalRevenue = g.Sum(il => il.TotalPrice)
            })
            .OrderByDescending(x => x.QuantitySold)
            .Take(count)
            .ToListAsync();

        return topProducts.Select(p => new TopProductDto(
            ProductId: p.ProductId,
            ProductName: p.ProductName,
            QuantitySold: p.QuantitySold,
            TotalRevenue: p.TotalRevenue
        )).ToList();
    }

    /// <summary>
    /// Get top customers by purchase value for the specified year and month
    /// </summary>
    public async Task<List<TopCustomerDto>> GetTopCustomersAsync(int year, int month, int count = 10)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Calculate date range for the selected month
        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1);

        // Get invoices from the selected month with customer info
        var invoicesInMonth = await dbContext.Invoices
            .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate < endDate)
            .Where(i => i.Status == "completed" || i.Status == "paid" || i.Status == "delivering")
            .Where(i => i.PersonId != null)
            .ToListAsync();

        // Group by customer
        var customerGroups = invoicesInMonth
            .GroupBy(i => i.PersonId!.Value)
            .Select(g => new
            {
                CustomerId = g.Key,
                OrderCount = g.Count(),
                TotalPurchaseValue = g.Sum(i => i.FinalAmount)
            })
            .OrderByDescending(x => x.TotalPurchaseValue)
            .Take(count)
            .ToList();

        if (!customerGroups.Any())
        {
            return new List<TopCustomerDto>();
        }

        // Load customer names
        var customerIds = customerGroups.Select(c => c.CustomerId).ToList();
        var persons = await dbContext.Persons
            .Where(p => customerIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName);

        return customerGroups.Select(c => new TopCustomerDto(
            CustomerId: c.CustomerId,
            CustomerName: persons.TryGetValue(c.CustomerId, out var name) ? name : "Unknown",
            OrderCount: c.OrderCount,
            TotalPurchaseValue: c.TotalPurchaseValue
        )).ToList();
    }
}
