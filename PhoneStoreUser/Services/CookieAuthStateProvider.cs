using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace PhoneStoreUser.Services;

public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private ClaimsPrincipal _cachedUser;

    public CookieAuthStateProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpUser = _httpContextAccessor.HttpContext?.User;
        if (httpUser is not null && httpUser.Identity?.IsAuthenticated == true)
        {
            _cachedUser = httpUser;
        }

        return Task.FromResult(new AuthenticationState(_cachedUser));
    }

    public void NotifyUserLogout()
    {
        _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }
}
