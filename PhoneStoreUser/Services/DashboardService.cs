using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;
using PhoneStoreUser.Services;
// Nhớ using namespace chứa AppDbContext và DashboardModel của bạn

public class DashboardService : IDashboardService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public DashboardService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<DashboardModel?> GetDashboardAsync(int personId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        // 1. Lấy thông tin cơ bản của Person
        var person = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null) return null;

        // 2. Lấy thống kê chi tiêu (Chỉ tính đơn đã thanh toán/hoàn thành)
        var completedInvoicesQuery = context.Invoices
            .AsNoTracking()
            .Where(i => i.PersonId == personId &&
                       (i.Status == "paid" || i.Status == "completed"));

        var totalSpent = await completedInvoicesQuery.SumAsync(i => i.FinalAmount);
        var totalOrders = await completedInvoicesQuery.CountAsync();

        // 3. Lấy danh sách 5 đơn hàng gần nhất (Lấy tất cả trạng thái để khách theo dõi)
        var recentOrders = await context.Invoices
            .AsNoTracking()
            .Where(i => i.PersonId == personId)
            .OrderByDescending(i => i.InvoiceDate)
            .Take(5)
            .Select(i => new OrderSimpleDto
            {
                InvoiceId = i.Id,
                Date = i.InvoiceDate.GetValueOrDefault(DateTime.MinValue),
                Status = i.Status, // Cần convert sang string nếu trong DB là Enum
                TotalAmount = i.FinalAmount
            })
            .ToListAsync();

        // 4. Lấy danh sách thiết bị & tính hạn bảo hành
        var myDevicesQuery =
        from il in context.InvoiceLines
        join i in context.Invoices on il.InvoiceId equals i.Id
        join ils in context.InvoiceLineSerials on il.Id equals ils.InvoiceLineId
        join ps in context.ProductSerials on ils.ProductSerialId equals ps.Id
        join p in context.Products on il.ProductId equals p.Id
        where i.PersonId == personId && (i.Status == "completed" || i.Status == "paid")
        orderby i.InvoiceDate descending
        select new
        {
            ProductName = p.Name,
            SerialOrImei = ps.Imei1 != null && ps.Imei1 != "" ? ps.Imei1 : ps.SerialNumber,
            PurchaseDate = i.InvoiceDate,
            WarrantyMonths = p.WarrantyMonths
        };

        var myDevicesRaw = await myDevicesQuery.ToListAsync();

        // Map sang DTO và tính toán logic hết hạn ở phía C# 
        var myDevices = myDevicesRaw.Select(d => new DeviceDto
        {
            ProductName = d.ProductName,
            IMEI = d.SerialOrImei,
            PurchaseDate = d.PurchaseDate.GetValueOrDefault(),
            WarrantyExpireDate = d.PurchaseDate.GetValueOrDefault().AddMonths(d.WarrantyMonths)
        }).ToList();

        // Đếm số thiết bị còn bảo hành
        var activeWarrantyCount = myDevices.Count(d => d.IsActive);

        // 5. Tổng hợp dữ liệu trả về Model
        return new DashboardModel
        {
            PersonId = person.Id,
            FullName = person.FullName,
            TotalSpent = totalSpent,
            TotalOrders = totalOrders,
            ActiveWarrantyCount = activeWarrantyCount,
            RecentOrders = recentOrders,
            MyDevices = myDevices
        };
    }
}
