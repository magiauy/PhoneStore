using System.Security.Claims;
using System.Text.Json;
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

// Single cookie scheme for both user and admin authentication
const string authScheme = "guzone.auth";
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
// Single cookie authentication scheme for both user and admin
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = authScheme;
        options.DefaultAuthenticateScheme = authScheme;
        options.DefaultChallengeScheme = authScheme;
        options.DefaultSignInScheme = authScheme;
        options.DefaultSignOutScheme = authScheme;
    })
    .AddCookie(authScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = authScheme;
        options.Cookie.HttpOnly = true;
        // Use SameAsRequest for development (HTTP) and production (HTTPS) compatibility
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        // Custom event to handle admin login path redirect
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                // If accessing admin pages, redirect to admin login
                if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
                {
                    context.RedirectUri = "/admin";
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireAuthenticatedUser();
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
    
    // Create principal with single cookie scheme including all claims
    var principal = CreatePrincipal(account, person, authScheme, isAdmin: false);
    
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

    await context.SignInAsync(authScheme, principal, authProperties);

    return Results.Redirect(finalReturnUrl);
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (HttpContext context) =>
{
    // Clear the single authentication cookie
    await context.SignOutAsync(authScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(authScheme);
    return Results.Ok();
}).AllowAnonymous();

app.MapGet("/logout", async (HttpContext context) =>
{
    // Clear the single authentication cookie
    await context.SignOutAsync(authScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(authScheme);
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

    // Verify password
    if (!PhoneStoreUser.Utils.PasswordHasher.VerifyPassword(model.Password, account.Password))
    {
        return Results.Redirect("/admin?error=invalid_credentials");
    }

    var person = await dbContext.Persons.FindAsync(account.PersonId);
    
    if (!string.Equals(person?.PersonType, "EMPLOYEE", StringComparison.OrdinalIgnoreCase))
    {
        return Results.Redirect("/admin?error=unauthorized");
    }

    // Create principal with single cookie scheme including admin role and permissions
    var principal = CreatePrincipal(account, person, authScheme, isAdmin: true);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        RedirectUri = "/admin/dashboard"
    };

    await context.SignInAsync(authScheme, principal, authProperties);

    return Results.Redirect("/admin/dashboard");
}).AllowAnonymous().DisableAntiforgery();

adminRoutes.MapPost("/logout", async (HttpContext context) =>
{
    // Clear the single authentication cookie
    await context.SignOutAsync(authScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(authScheme);
    return Results.Ok();
}).AllowAnonymous();

adminRoutes.MapGet("/logout", async (HttpContext context) =>
{
    // Clear the single authentication cookie
    await context.SignOutAsync(authScheme);
    // Also delete the cookie explicitly to ensure it's removed
    context.Response.Cookies.Delete(authScheme);
    return Results.Redirect("/admin");
}).AllowAnonymous();

// Map controllers for API endpoints (PayOS callback)
app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

ClaimsPrincipal CreatePrincipal(AccountEntity account, PersonEntity? person, string authenticationScheme, bool isAdmin = false)
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

    // Determine role based on person type
    var roleName = string.Equals(person?.PersonType, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
        ? adminRoleName
        : person?.PersonType ?? "Customer";

    claims.Add(new Claim(ClaimTypes.Role, roleName));
    
    // Add is_admin claim to indicate admin context login
    claims.Add(new Claim("is_admin", isAdmin.ToString().ToLower()));
    
    // Add permissions claim (serialized as JSON)
    // For admin users, include admin permissions; for regular users, include basic permissions
    var permissions = isAdmin && string.Equals(person?.PersonType, "EMPLOYEE", StringComparison.OrdinalIgnoreCase)
        ? new List<string> { "admin.access", "admin.dashboard", "admin.orders", "admin.customers", "admin.products", "admin.reports" }
        : new List<string> { "user.profile", "user.orders", "user.cart" };
    
    claims.Add(new Claim("permissions", JsonSerializer.Serialize(permissions)));

    var identity = new ClaimsIdentity(claims, authenticationScheme);
    return new ClaimsPrincipal(identity);
}
