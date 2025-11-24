using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminOrderHistoryService
{
    Task<OrderHistoryPageDto> GetOrderHistoryAsync(int page, int pageSize, string? search, string? statusFilter);
    Task<OrderStatisticsDto> GetOrderStatisticsAsync(DateTime? startDate, DateTime? endDate);
}
