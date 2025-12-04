using PhoneStore.Services.ViewModels;

namespace PhoneStore.Services.Interfaces
{
    /// <summary>
    /// Service for dashboard statistics
    /// </summary>
    public interface IDashboardService
    {
        /// <summary>
        /// Get dashboard data for a specific month
        /// </summary>
        DashboardMonthlyData GetMonthlyData(int year, int month);

        /// <summary>
        /// Get daily revenue for chart
        /// </summary>
        List<DailyRevenueViewModel> GetDailyRevenue(int year, int month);

        /// <summary>
        /// Get top selling products
        /// </summary>
        List<TopProductViewModel> GetTopProducts(int year, int month, int count = 10);

        /// <summary>
        /// Get top customers by spending
        /// </summary>
        List<TopCustomerViewModel> GetTopCustomers(int year, int month, int count = 10);
    }
}
