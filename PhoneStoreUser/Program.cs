using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;
using PhoneStoreUser.Data.Enums;
using PhoneStoreUser.Models;
using PhoneStoreUser.Services;

var builder = WebApplication.CreateBuilder(args);

const string defaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
const string adminScheme = "guzone.admin";
const string adminRoleName = nameof(PersonType.EMPLOYEE);

// Configure PayOS
builder.Services.Configure<PayOSConfig>(builder.Configuration.GetSection("PayOS"));

// Add services to the container.
builder.Services.AddControllers(); // Enable API controllers for PayOS callback
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<DataSource>(sp => new DataSource(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminOrderHistoryService, AdminOrderHistoryService>();
builder.Services.AddScoped<IAdminCustomerService, AdminCustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IPayOSService, PayOSService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<IAdminPromotionService, AdminPromotionService>();
builder.Services.AddScoped<IAdminReviewService, AdminReviewService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CookieAuthStateProvider>());
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "guzone.policy";
        options.DefaultAuthenticateScheme = "guzone.policy";
        options.DefaultChallengeScheme = "guzone.policy";
    })
    .AddPolicyScheme("guzone.policy", "GuZone policy", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            // 1. Path-based check for admin pages (direct page requests)
            if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
            {
                return adminScheme;
            }
            
            // 2. For SignalR/Blazor connections, we need to determine context
            if (context.Request.Path.StartsWithSegments("/_blazor", StringComparison.OrdinalIgnoreCase))
            {
                // Check Referer header first (most reliable for Blazor)
                var referer = context.Request.Headers.Referer.ToString();
                if (!string.IsNullOrEmpty(referer))
                {
                    // Parse the referer to get the path
                    if (Uri.TryCreate(referer, UriKind.Absolute, out var refererUri))
                    {
                        if (refererUri.AbsolutePath.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
                        {
                            return adminScheme;
                        }
                        // Referer exists but not admin path - use default scheme
                        return defaultScheme;
                    }
                }
                
                // No referer - check cookies as fallback
                // If ONLY admin cookie exists, use admin scheme
                var hasAdminCookie = context.Request.Cookies.ContainsKey(adminScheme);
                var hasUserCookie = context.Request.Cookies.ContainsKey("guzone.auth");
                
                if (hasAdminCookie && !hasUserCookie)
                {
                    return adminScheme;
                }
                
                // If both cookies exist or only user cookie, default to user scheme
                // The page context (admin vs user) should be determined by Referer
                return defaultScheme;
            }
            
            // 3. All other pages (user pages) should use default scheme
            return defaultScheme;
        };
    })
    .AddCookie(defaultScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "guzone.auth";
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest for development (HTTP) and production (HTTPS) compatibility
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    })
    .AddCookie(adminScheme, options =>
    {
        options.LoginPath = "/admin";
        options.AccessDeniedPath = "/admin";
        options.Cookie.Name = adminScheme;
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest for development (HTTP) and production (HTTPS) compatibility
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddAuthenticationSchemes(adminScheme);
        policy.RequireRole(adminRoleName);
    });
});

// EF Core DbContext for read operations (e.g., Brand names)
var dbSection = builder.Configuration.GetSection("Database");
var host = dbSection["Host"] ?? "localhost";
var port = dbSection["Port"] ?? "3306";
var database = dbSection["Database"] ?? "bandienthoai";
var user = dbSection["User"] ?? "root";
var password = dbSection["Password"] ?? string.Empty;
var connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};AllowZeroDateTime=True;ConvertZeroDateTime=True;";

builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/login", async (
    [Microsoft.AspNetCore.Mvc.FromForm] LoginModel model,
    HttpContext context,
    IDbContextFactory<AppDbContext> dbContextFactory) =>
{
    if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
    {
        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        var redirectUrl = string.IsNullOrEmpty(returnUrl) 
            ? "/login?error=missing_credentials" 
            : $"/login?error=missing_credentials&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(redirectUrl);
    }

    using var dbContext = await dbContextFactory.CreateDbContextAsync();

    // Find account
    var account = await dbContext.Accounts.FirstOrDefaultAsync(a =>
        (a.Username == model.EmailOrUsername.Trim()) && a.IsActive);

    if (account is null)
    {
        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        var redirectUrl = string.IsNullOrEmpty(returnUrl) 
            ? "/login?error=invalid_credentials" 
            : $"/login?error=invalid_credentials&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(redirectUrl);
    }

    // Verify password
    if (!PhoneStoreUser.Utils.PasswordHasher.VerifyPassword(model.Password, account.Password))
    {
        var returnUrl = context.Request.Query["ReturnUrl"].ToString();
        var redirectUrl = string.IsNullOrEmpty(returnUrl) 
            ? "/login?error=invalid_credentials" 
            : $"/login?error=invalid_credentials&ReturnUrl={Uri.EscapeDataString(returnUrl)}";
        return Results.Redirect(redirectUrl);
    }

    var person = await dbContext.Persons.FindAsync(account.PersonId);
    var principal = CreatePrincipal(account, person, defaultScheme);
    
    // Get ReturnUrl from query string or use default
    var finalReturnUrl = context.Request.Query["ReturnUrl"].ToString();
    if (string.IsNullOrEmpty(finalReturnUrl) || !finalReturnUrl.StartsWith("/"))
    {
        finalReturnUrl = "/";
    }
    
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        RedirectUri = finalReturnUrl
    };

    await context.SignInAsync(defaultScheme, principal, authProperties);

    return Results.Redirect(finalReturnUrl);
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (HttpContext context) =>
{
    // Clear the authentication cookie
    await context.SignOutAsync(defaultScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete("guzone.auth");
    return Results.Ok();
}).AllowAnonymous();

app.MapGet("/logout", async (HttpContext context) =>
{
    // Clear the authentication cookie
    await context.SignOutAsync(defaultScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete("guzone.auth");
    return Results.Redirect("/");
}).AllowAnonymous();

var adminRoutes = app.MapGroup("/admin");
adminRoutes.RequireAuthorization("AdminOnly");

adminRoutes.MapPost("/login", async (
    [Microsoft.AspNetCore.Mvc.FromForm] LoginModel model,
    HttpContext context,
    IDbContextFactory<AppDbContext> dbContextFactory) =>
{
    if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
    {
        return Results.Redirect("/admin?error=missing_credentials");
    }

    using var dbContext = await dbContextFactory.CreateDbContextAsync();

    // Find account
    var account = await dbContext.Accounts.FirstOrDefaultAsync(a =>
        (a.Username == model.EmailOrUsername.Trim()) && a.IsActive);

    if (account is null)
    {
        return Results.Redirect("/admin?error=invalid_credentials");
    }
    Console.WriteLine(account?.Username);
    // Verify password
    if (!PhoneStoreUser.Utils.PasswordHasher.VerifyPassword(model.Password, account.Password))
    {
        return Results.Redirect("/admin?error=invalid_credentials");
    }
    Console.WriteLine(account?.PersonId);
    var person = await dbContext.Persons.FindAsync(account.PersonId);
    Console.WriteLine(person?.PersonType);
    if (!string.Equals(person?.PersonType, "EMPLOYEE", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/admin?error=unauthorized");
    }

    var principal = CreatePrincipal(account, person, adminScheme);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        RedirectUri = "/admin/dashboard"
    };

    await context.SignInAsync(adminScheme, principal, authProperties);

    return Results.Redirect("/admin/dashboard");
}).AllowAnonymous().DisableAntiforgery();

adminRoutes.MapPost("/logout", async (HttpContext context) =>
{
    // Clear the admin authentication cookie
    await context.SignOutAsync(adminScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(adminScheme);
    return Results.Ok();
}).AllowAnonymous();

adminRoutes.MapGet("/logout", async (HttpContext context) =>
{
    // Clear the admin authentication cookie
    await context.SignOutAsync(adminScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(adminScheme);
    return Results.Redirect("/admin");
}).AllowAnonymous();

// Map controllers for API endpoints (PayOS callback)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

ClaimsPrincipal CreatePrincipal(AccountEntity account, PersonEntity? person, string authenticationScheme)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, account.Id.ToString()),
        new(ClaimTypes.Name, account.Username),
        new("person_id", account.PersonId.ToString())
    };

    if (!string.IsNullOrWhiteSpace(person?.Email))
    {
        claims.Add(new Claim(ClaimTypes.Email, person.Email));
    }

    var roleName = string.Equals(person?.PersonType, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
        ? adminRoleName
        : person?.PersonType ?? "Customer";

    claims.Add(new Claim(ClaimTypes.Role, roleName));

    var identity = new ClaimsIdentity(claims, authenticationScheme);
    return new ClaimsPrincipal(identity);
}
