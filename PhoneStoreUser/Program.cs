using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreUser.Components;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;
using PhoneStoreUser.Services;

var builder = WebApplication.CreateBuilder(args);

const string defaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
const string adminScheme = "guzone.admin";
const string adminRoleName = nameof(PersonType.EMPLOYEE);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<DataSource>(sp => new DataSource(sp.GetRequiredService<IConfiguration>()));
builder.Services.AddScoped<IProductModelRepository, ProductModelRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductModelAttributeRepository, ProductModelAttributeRepository>();
builder.Services.AddScoped<IProductAttributeRepository, ProductAttributeRepository>();
builder.Services.AddScoped<IProductAttributeValueRepository, ProductAttributeValueRepository>();
builder.Services.AddScoped<IProductAttributeOptionRepository, ProductAttributeOptionRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IPersonRepository, PersonRepository>();
builder.Services.AddScoped<IProductCatalogService, ProductCatalogService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IUserProfileService, UserProfileService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAdminOrderService, AdminOrderService>();
builder.Services.AddScoped<IAdminOrderHistoryService, AdminOrderHistoryService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<CookieAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();
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
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
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
    IAuthRepository authRepository,
    IPersonRepository personRepository) =>
{
    if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
    {
        return Results.Redirect("/login?error=missing_credentials");
    }

    var account = await authRepository.AuthenticateAsync(model.EmailOrUsername.Trim(), model.Password);
    if (account is null)
    {
        return Results.Redirect("/login?error=invalid_credentials");
    }

    var person = await personRepository.GetByIdAsync(account.PersonId);
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
    IAuthRepository authRepository,
    IPersonRepository personRepository) =>
{
    if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
    {
        return Results.Redirect("/admin/login?error=missing_credentials");
    }

    var account = await authRepository.AuthenticateAsync(model.EmailOrUsername.Trim(), model.Password);
    if (account is null)
    {
        return Results.Redirect("/admin/login?error=invalid_credentials");
    }

    var person = await personRepository.GetByIdAsync(account.PersonId);
    if (person?.PersonType != PersonType.EMPLOYEE)
    {
        return Results.Redirect("/admin/login?error=unauthorized");
    }

    var principal = CreatePrincipal(account, person, adminScheme);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe,
        RedirectUri = "/admin"
    };

    await context.SignInAsync(adminScheme, principal, authProperties);

    return Results.Redirect("/admin");
}).AllowAnonymous().DisableAntiforgery();

adminRoutes.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(adminScheme);
    return Results.Ok();
}).RequireAuthorization("AdminOnly");

adminRoutes.MapGet("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(adminScheme);
    return Results.Redirect("/admin/login");
}).RequireAuthorization("AdminOnly");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

ClaimsPrincipal CreatePrincipal(PhoneStoreRepository.Models.Account account, PhoneStoreRepository.Models.Person? person, string authenticationScheme)
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

    var roleName = person?.PersonType == PersonType.EMPLOYEE
        ? adminRoleName
        : person?.PersonType.ToString() ?? "Customer";

    claims.Add(new Claim(ClaimTypes.Role, roleName));

    var identity = new ClaimsIdentity(claims, authenticationScheme);
    return new ClaimsPrincipal(identity);
}
