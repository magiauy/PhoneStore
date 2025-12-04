using System.ComponentModel.DataAnnotations;

namespace PhoneStoreUser.Components.Models;

public class DashboardModel
    {
        public int PersonId { get; set; }
        public string FullName { get; set; }

        // Các chỉ số thống kê
        public decimal TotalSpent { get; set; } = 0;
        public int TotalOrders { get; set; } = 0;
        public int ActiveWarrantyCount { get; set; } = 0;
    public List<OrderSimpleDto> RecentOrders { get; internal set; }
    public List<DeviceDto> MyDevices { get; internal set; }
}
public class OrderSimpleDto
{
    public int InvoiceId { get; set; }
    public DateTime Date { get; set; }
    public string Status { get; set; }
    public decimal TotalAmount { get; set; }
}

public class DeviceDto
{
    public string ProductName { get; set; }
    public string IMEI { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime WarrantyExpireDate { get; set; }

    // Logic kiểm tra còn hạn hay không
    public bool IsActive => DateTime.Now < WarrantyExpireDate;
}
