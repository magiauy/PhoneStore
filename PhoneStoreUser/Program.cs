using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhoneStoreRepository.Data;
using PhoneStoreRepository.Repositories.Implementations;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreUser.Components;
using PhoneStoreUser.Data;
using PhoneStoreUser.Services;
using PhoneStoreUser.Components.Models;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthStateProvider>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "guzone.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

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
    LoginModel model,
    HttpContext context,
    IAuthRepository authRepository,
    IPersonRepository personRepository) =>
{
    if (string.IsNullOrWhiteSpace(model.EmailOrUsername) || string.IsNullOrWhiteSpace(model.Password))
    {
        return Results.BadRequest();
    }

    var account = await authRepository.AuthenticateAsync(model.EmailOrUsername.Trim(), model.Password);
    if (account is null)
    {
        return Results.Unauthorized();
    }

    var person = await personRepository.GetByIdAsync(account.PersonId);
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

    claims.Add(new Claim(ClaimTypes.Role, person?.PersonType.ToString() ?? "Customer"));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    var authProperties = new AuthenticationProperties
    {
        IsPersistent = model.RememberMe
    };

    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
    return Results.Ok(new
    {
        account.Id,
        account.Username,
        account.PersonId,
        person?.FullName,
        person?.Email
    });
}).AllowAnonymous();

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
}).RequireAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
