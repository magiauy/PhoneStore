using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface IAuthService
{
    Task ChangePasswordAsync(int accountId, ChangePasswordModel model);
}
