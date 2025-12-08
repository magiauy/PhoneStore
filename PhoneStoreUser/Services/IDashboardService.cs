using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

/// <summary>
/// Service interface for dashboard statistics and analytics
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Get monthly statistics for the specified year and month
    /// </summary>
    Task<MonthlyStatistics> GetMonthlyStatisticsAsync(int year, int month);

    /// <summary>
    /// Get top selling products for the specified year and month
    /// </summary>
    Task<List<TopProductDto>> GetTopSellingProductsAsync(int year, int month, int count = 10);

    /// <summary>
    /// Get top customers by purchase value for the specified year and month
    /// </summary>
    Task<List<TopCustomerDto>> GetTopCustomersAsync(int year, int month, int count = 10);
}
