using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IAdminCustomerService
{
    Task<PagedResult<AdminCustomerDto>> GetCustomersAsync(int page, int pageSize);
    Task CreateCustomerAsync(CreateCustomerDto dto);
}
