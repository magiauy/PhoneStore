using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace PhoneStoreUser.Services;

/// <summary>
/// Context-aware authentication state provider that selects the correct user
/// based on the current page URL (admin vs user pages).
/// </summary>
public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager? _navigationManager;
    private ClaimsPrincipal _cachedUser;
    
    private const string UserScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    private const string AdminScheme = "guzone.admin";

    public CookieAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        NavigationManager? navigationManager = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _navigationManager = navigationManager;
        _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return Task.FromResult(new AuthenticationState(_cachedUser));
        }

        // Determine if we're in admin context
        var isAdminContext = IsAdminContext(httpContext);
        
        // Get the appropriate user based on context
        var user = GetUserForContext(httpContext, isAdminContext);
        
        if (user is not null && user.Identity?.IsAuthenticated == true)
        {
            _cachedUser = user;
        }
        else
        {
            _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return Task.FromResult(new AuthenticationState(_cachedUser));
    }

    /// <summary>
    /// Determines if the current request is in admin context
    /// Uses NavigationManager for accurate URL in Blazor Server
    /// </summary>
    private bool IsAdminContext(HttpContext httpContext)
    {
        // 1. First priority: Use NavigationManager to get the actual page URL
        //    This works correctly even during SignalR connections
        if (_navigationManager is not null)
        {
            try
            {
                var uri = new Uri(_navigationManager.Uri);
                if (uri.AbsolutePath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                // NavigationManager has a valid non-admin URL
                return false;
            }
            catch
            {
                // NavigationManager might not be initialized yet, fall through
            }
        }
        
        // 2. Fallback: Check the request path directly
        var path = httpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // 3. For SignalR, check Referer header
        if (path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase))
        {
            var referer = httpContext.Request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer) && Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
            {
                return refererUri.AbsolutePath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase);
            }
        }
        
        return false;
    }

    /// <summary>
    /// Gets the appropriate ClaimsPrincipal based on context.
    /// </summary>
    private ClaimsPrincipal? GetUserForContext(HttpContext httpContext, bool isAdminContext)
    {
        var httpUser = httpContext.User;
        
        if (httpUser?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        
        var authenticationType = httpUser.Identity.AuthenticationType;
        
        if (isAdminContext)
        {
            // For admin context, only accept admin-authenticated users
            if (string.Equals(authenticationType, AdminScheme, StringComparison.OrdinalIgnoreCase))
            {
                return httpUser;
            }
            return null;
        }
        else
        {
            // For user context, only accept user-authenticated users
            if (string.Equals(authenticationType, UserScheme, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(authenticationType, "Cookies", StringComparison.OrdinalIgnoreCase))
            {
                return httpUser;
            }
            return null;
        }
    }

    public void NotifyUserLogout()
    {
        _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }

    public void NotifyUserLogin()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var isAdminContext = IsAdminContext(httpContext);
            var user = GetUserForContext(httpContext, isAdminContext);
            _cachedUser = user ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }

    public void RefreshAuthenticationState()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var isAdminContext = IsAdminContext(httpContext);
            var user = GetUserForContext(httpContext, isAdminContext);
            _cachedUser = user ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }
}
