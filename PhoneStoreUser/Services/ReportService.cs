using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service implementation for generating business reports
/// </summary>
public class ReportService : IReportService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ReportService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <summary>
    /// Get purchase order report data for the specified date range
    /// </summary>
    public async Task<PurchaseOrderReportData> GetPurchaseOrderReportAsync(DateTime startDate, DateTime endDate)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Normalize dates to include the full end date
        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date.AddDays(1);

        // Get purchase orders within the date range
        var purchaseOrders = await dbContext.PurchaseOrders
            .Include(po => po.Supplier)
            .Include(po => po.Lines)
            .Where(po => po.OrderDate >= normalizedStartDate && po.OrderDate < normalizedEndDate)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

        // Calculate totals
        var totalCount = purchaseOrders.Count;
        var totalValue = purchaseOrders.Sum(po => po.TotalAmount);

        // Generate chart data grouped by day/week/month based on date range span
        var chartData = GenerateChartData(purchaseOrders, normalizedStartDate, normalizedEndDate,
            po => po.OrderDate, po => po.TotalAmount);

        // Map to report items
        var items = purchaseOrders.Select(po => new PurchaseOrderReportItem(
            Id: po.Id,
            OrderDate: po.OrderDate,
            SupplierName: po.Supplier?.Name ?? "Unknown",
            TotalItems: po.Lines.Sum(l => l.Quantity),
            TotalValue: po.TotalAmount,
            Status: po.Status
        )).ToList();

        return new PurchaseOrderReportData(
            TotalCount: totalCount,
            TotalValue: totalValue,
            ChartData: chartData,
            Items: items
        );
    }


    /// <summary>
    /// Get invoice report data for the specified date range
    /// </summary>
    public async Task<InvoiceReportData> GetInvoiceReportAsync(DateTime startDate, DateTime endDate)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Normalize dates to include the full end date
        var normalizedStartDate = startDate.Date;
        var normalizedEndDate = endDate.Date.AddDays(1);

        // Get invoices within the date range
        var invoices = await dbContext.Invoices
            .Include(i => i.Lines)
            .Where(i => i.InvoiceDate != null && i.InvoiceDate >= normalizedStartDate && i.InvoiceDate < normalizedEndDate)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();

        // Get customer names for the invoices
        var personIds = invoices.Where(i => i.PersonId.HasValue).Select(i => i.PersonId!.Value).Distinct().ToList();
        var persons = await dbContext.Persons
            .Where(p => personIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.FullName);

        // Calculate totals
        var totalCount = invoices.Count;
        var totalRevenue = invoices.Sum(i => i.FinalAmount);

        // Generate chart data grouped by day/week/month based on date range span
        var chartData = GenerateChartData(invoices, normalizedStartDate, normalizedEndDate,
            i => i.InvoiceDate ?? DateTime.MinValue, i => i.FinalAmount);

        // Map to report items
        var items = invoices.Select(i => new InvoiceReportItem(
            Id: i.Id,
            InvoiceDate: i.InvoiceDate ?? DateTime.MinValue,
            CustomerName: i.PersonId.HasValue && persons.TryGetValue(i.PersonId.Value, out var name) ? name : "Khách lẻ",
            TotalItems: i.Lines.Sum(l => l.Quantity),
            TotalValue: i.FinalAmount,
            Status: i.Status
        )).ToList();

        return new InvoiceReportData(
            TotalCount: totalCount,
            TotalRevenue: totalRevenue,
            ChartData: chartData,
            Items: items
        );
    }

    /// <summary>
    /// Generate chart data points grouped by appropriate time period based on date range
    /// </summary>
    private static List<ChartDataPoint> GenerateChartData<T>(
        List<T> items,
        DateTime startDate,
        DateTime endDate,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> valueSelector)
    {
        var daySpan = (endDate - startDate).Days;

        // Determine grouping strategy based on date range
        // <= 31 days: group by day
        // <= 90 days: group by week
        // > 90 days: group by month
        if (daySpan <= 31)
        {
            return GroupByDay(items, startDate, endDate, dateSelector, valueSelector);
        }
        else if (daySpan <= 90)
        {
            return GroupByWeek(items, startDate, endDate, dateSelector, valueSelector);
        }
        else
        {
            return GroupByMonth(items, startDate, endDate, dateSelector, valueSelector);
        }
    }

    private static List<ChartDataPoint> GroupByDay<T>(
        List<T> items,
        DateTime startDate,
        DateTime endDate,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> valueSelector)
    {
        var result = new List<ChartDataPoint>();
        var currentDate = startDate.Date;

        while (currentDate < endDate)
        {
            var dayItems = items.Where(i => dateSelector(i).Date == currentDate).ToList();
            result.Add(new ChartDataPoint(
                Label: currentDate.ToString("dd/MM"),
                Value: dayItems.Sum(valueSelector),
                Count: dayItems.Count
            ));
            currentDate = currentDate.AddDays(1);
        }

        return result;
    }

    private static List<ChartDataPoint> GroupByWeek<T>(
        List<T> items,
        DateTime startDate,
        DateTime endDate,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> valueSelector)
    {
        var result = new List<ChartDataPoint>();
        var currentWeekStart = startDate.Date;

        while (currentWeekStart < endDate)
        {
            var weekEnd = currentWeekStart.AddDays(7);
            if (weekEnd > endDate) weekEnd = endDate;

            var weekItems = items.Where(i =>
            {
                var itemDate = dateSelector(i).Date;
                return itemDate >= currentWeekStart && itemDate < weekEnd;
            }).ToList();

            result.Add(new ChartDataPoint(
                Label: $"{currentWeekStart:dd/MM} - {weekEnd.AddDays(-1):dd/MM}",
                Value: weekItems.Sum(valueSelector),
                Count: weekItems.Count
            ));

            currentWeekStart = weekEnd;
        }

        return result;
    }

    private static List<ChartDataPoint> GroupByMonth<T>(
        List<T> items,
        DateTime startDate,
        DateTime endDate,
        Func<T, DateTime> dateSelector,
        Func<T, decimal> valueSelector)
    {
        var result = new List<ChartDataPoint>();
        var currentMonthStart = new DateTime(startDate.Year, startDate.Month, 1);

        while (currentMonthStart < endDate)
        {
            var monthEnd = currentMonthStart.AddMonths(1);

            var monthItems = items.Where(i =>
            {
                var itemDate = dateSelector(i);
                return itemDate >= currentMonthStart && itemDate < monthEnd;
            }).ToList();

            result.Add(new ChartDataPoint(
                Label: currentMonthStart.ToString("MM/yyyy"),
                Value: monthItems.Sum(valueSelector),
                Count: monthItems.Count
            ));

            currentMonthStart = monthEnd;
        }

        return result;
    }
}
