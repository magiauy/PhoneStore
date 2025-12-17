using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminCustomerService
{
    Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(int page, int pageSize, string? searchTerm = null);
    Task CreateCustomerAsync(CreateCustomerDto dto);
    Task<AdminCustomerDto?> GetCustomerByIdAsync(int id);
    Task UpdateCustomerAsync(int id, UpdateCustomerDto dto);
    Task<CustomerAccountInfoDto?> GetCustomerAccountAsync(int personId);
    Task<bool> UpdateAccountPasswordAsync(int personId, string newPassword);
    Task<bool> ToggleAccountStatusAsync(int personId);
}
