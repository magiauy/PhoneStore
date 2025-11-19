using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface IUserSessionService
{
    UserSession? CurrentSession { get; }
    event Action? OnChange;

    Task<UserSession?> InitializeAsync();
    Task<UserSession?> LoginAsync(string identifier, string password, bool rememberMe);
    Task LogoutAsync();
}
