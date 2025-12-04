namespace PhoneStore.Services.ViewModels
{
    /// <summary>
    /// Daily revenue data for chart
    /// </summary>
    public class DailyRevenueViewModel
    {
        public DateTime Date { get; set; }
        public int Day => Date.Day;
        public decimal Revenue { get; set; }
        public int InvoiceCount { get; set; }
        
        public string DayDisplay => Date.Day.ToString();
        public string RevenueDisplay => Revenue.ToString("N0") + " ₫";
    }

    /// <summary>
    /// Top selling product statistics
    /// </summary>
    public class TopProductViewModel
    {
        public int Rank { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSku { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
        
        public string TotalRevenueDisplay => TotalRevenue.ToString("N0") + " ₫";
        public string QuantityDisplay => QuantitySold.ToString("N0");
    }

    /// <summary>
    /// Top customer by spending
    /// </summary>
    public class TopCustomerViewModel
    {
        public int Rank { get; set; }
        public int? PersonId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int InvoiceCount { get; set; }
        public decimal TotalSpent { get; set; }
        
        public string TotalSpentDisplay => TotalSpent.ToString("N0") + " ₫";
        public string InvoiceCountDisplay => InvoiceCount.ToString();
    }

    /// <summary>
    /// Dashboard summary for a month
    /// </summary>
    public class DashboardMonthlyData
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalInvoices { get; set; }
        public int TotalProducts { get; set; }
        public int TotalCustomers { get; set; }
        public List<DailyRevenueViewModel> DailyRevenues { get; set; } = new();
        public List<TopProductViewModel> TopProducts { get; set; } = new();
        public List<TopCustomerViewModel> TopCustomers { get; set; } = new();
        
        public string TotalRevenueDisplay => TotalRevenue.ToString("N0") + " ₫";
    }
}
