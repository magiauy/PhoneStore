using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace PhoneStoreUser.Services;

/// <summary>
/// Single cookie authentication state provider that works with one cookie scheme.
/// Determines admin context from claims (role and is_admin) instead of cookie scheme.
/// </summary>
public class CookieAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly NavigationManager? _navigationManager;
    private ClaimsPrincipal _cachedUser;
    
    private const string AuthScheme = "guzone.auth";
    private const string AdminRoleName = "EMPLOYEE";

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

        // Get the user from the single cookie
        var user = GetAuthenticatedUser(httpContext);
        
        if (user is not null && user.Identity?.IsAuthenticated == true)
        {
            // Determine if we're in admin context based on URL
            var isAdminContext = IsAdminContext(httpContext);
            
            // For admin context, verify user has admin role
            if (isAdminContext && !IsAdminUser(user))
            {
                _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
            }
            else
            {
                _cachedUser = user;
            }
        }
        else
        {
            _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        }

        return Task.FromResult(new AuthenticationState(_cachedUser));
    }

    /// <summary>
    /// Determines if the current request is in admin context based on URL path.
    /// </summary>
    public bool IsAdminContext(HttpContext? httpContext = null)
    {
        httpContext ??= _httpContextAccessor.HttpContext;
        
        // 1. First priority: Use NavigationManager to get the actual page URL
        if (_navigationManager is not null)
        {
            try
            {
                var uri = new Uri(_navigationManager.Uri);
                if (uri.AbsolutePath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                return false;
            }
            catch
            {
                // NavigationManager might not be initialized yet, fall through
            }
        }
        
        if (httpContext is null)
        {
            return false;
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
    /// Checks if the user has admin role from claims.
    /// </summary>
    public bool IsAdminUser(ClaimsPrincipal? user = null)
    {
        user ??= _cachedUser;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            return false;
        }
        
        // Check role claim for admin role
        var roleClaim = user.FindFirst(ClaimTypes.Role);
        if (roleClaim is not null && string.Equals(roleClaim.Value, AdminRoleName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        // Also check is_admin claim
        var isAdminClaim = user.FindFirst("is_admin");
        if (isAdminClaim is not null && string.Equals(isAdminClaim.Value, "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Gets the user's permissions from claims.
    /// </summary>
    public List<string> GetUserPermissions(ClaimsPrincipal? user = null)
    {
        user ??= _cachedUser;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new List<string>();
        }
        
        var permissionsClaim = user.FindFirst("permissions");
        if (permissionsClaim is null || string.IsNullOrEmpty(permissionsClaim.Value))
        {
            return new List<string>();
        }
        
        try
        {
            return JsonSerializer.Deserialize<List<string>>(permissionsClaim.Value) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    public bool HasPermission(string permission, ClaimsPrincipal? user = null)
    {
        var permissions = GetUserPermissions(user);
        return permissions.Contains(permission, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the authenticated user from the single cookie.
    /// </summary>
    private ClaimsPrincipal? GetAuthenticatedUser(HttpContext httpContext)
    {
        var httpUser = httpContext.User;
        
        if (httpUser?.Identity?.IsAuthenticated != true)
        {
            return null;
        }
        
        // Accept users authenticated with our single cookie scheme
        var authenticationType = httpUser.Identity.AuthenticationType;
        if (string.Equals(authenticationType, AuthScheme, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(authenticationType, "Cookies", StringComparison.OrdinalIgnoreCase))
        {
            return httpUser;
        }
        
        return null;
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
            var user = GetAuthenticatedUser(httpContext);
            _cachedUser = user ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }

    public void RefreshAuthenticationState()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var user = GetAuthenticatedUser(httpContext);
            _cachedUser = user ?? new ClaimsPrincipal(new ClaimsIdentity());
        }
        else
        {
            _cachedUser = new ClaimsPrincipal(new ClaimsIdentity());
        }
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_cachedUser)));
    }
}
