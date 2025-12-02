using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminOrderHistoryService
{
    Task<OrderHistoryPageDto> GetOrderHistoryAsync(int page, int pageSize, string? search, string? statusFilter, DateTime? startDate = null, DateTime? endDate = null);
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate, DateTime? endDate);
}
