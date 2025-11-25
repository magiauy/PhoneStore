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
using PhoneStoreUser.Services;

var builder = WebApplication.CreateBuilder(args);

const string defaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
const string adminScheme = "guzone.admin";
const string adminRoleName = nameof(PersonType.EMPLOYEE);

// Add services to the container.
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
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminOrderHistoryService, AdminOrderHistoryService>();
builder.Services.AddScoped<IAdminCustomerService, AdminCustomerService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
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
            return context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase)
                ? adminScheme
                : defaultScheme;
        };
    })
    .AddCookie(defaultScheme, options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "guzone.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    })
    .AddCookie(adminScheme, options =>
    {
        options.LoginPath = "/admin";
        options.AccessDeniedPath = "/admin";
        options.Cookie.Name = adminScheme;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
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
var connectionString = $"Server={host};Port={port};Database={database};User={user};Password={password};";

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
        return Results.Redirect("/login?error=missing_credentials");
    }

    using var dbContext = await dbContextFactory.CreateDbContextAsync();

    // Find account
    var account = await dbContext.Accounts.FirstOrDefaultAsync(a =>
        (a.Username == model.EmailOrUsername.Trim()) && a.IsActive);

    if (account is null)
    {
        return Results.Redirect("/login?error=invalid_credentials");
    }

    // Verify password
    if (!PhoneStoreUser.Utils.PasswordHasher.VerifyPassword(model.Password, account.Password))
    {
        return Results.Redirect("/login?error=invalid_credentials");
    }

    var person = await dbContext.Persons.FindAsync(account.PersonId);
    var principal = CreatePrincipal(account, person, defaultScheme);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        RedirectUri = "/"
    };

    await context.SignInAsync(defaultScheme, principal, authProperties);

    return Results.Redirect("/");
}).AllowAnonymous().DisableAntiforgery();

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(defaultScheme);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(defaultScheme);
    return Results.Redirect("/");
}).RequireAuthorization();

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
    await context.SignOutAsync(adminScheme);
    return Results.Ok();
}).RequireAuthorization("AdminOnly");

adminRoutes.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(adminScheme);
    return Results.Redirect("/admin");
}).RequireAuthorization("AdminOnly");

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
