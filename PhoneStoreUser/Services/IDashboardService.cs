using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services
{
    public interface IDashboardService
    {
        Task<DashboardModel?> GetDashboardAsync(int personId);
    }
}
