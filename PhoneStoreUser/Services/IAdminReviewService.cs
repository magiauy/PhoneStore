using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IAdminReviewService
{
    Task<PagedResult<ReviewEntity>> GetReviewsAsync(string? search, int page = 1, int pageSize = 10);
    Task<bool> DeleteReviewAsync(int id);
}
