using System.Text.Json;
using Microsoft.JSInterop;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public class UserSessionService : IUserSessionService, IAsyncDisposable
{
    private const string AuthModulePath = "./js/auth.js";
    private const int RememberMeDays = 30;
    private const int DefaultSessionDays = 1;

    private readonly IAuthRepository _authRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _authModule;
    private bool _hasLoadedFromCookie;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public UserSession? CurrentSession { get; private set; }

    public event Action? OnChange;

    public UserSessionService(
        IAuthRepository authRepository,
        IPersonRepository personRepository,
        IJSRuntime jsRuntime)
    {
        _authRepository = authRepository;
        _personRepository = personRepository;
        _jsRuntime = jsRuntime;
    }

    public async Task<UserSession?> InitializeAsync()
    {
        if (_hasLoadedFromCookie)
        {
            return CurrentSession;
        }

        if (!await TryEnsureModuleAsync())
        {
            return CurrentSession;
        }

        try
        {
            var sessionJson = await _authModule!.InvokeAsync<string?>("getUserCookie");
            if (!string.IsNullOrWhiteSpace(sessionJson))
            {
                CurrentSession = JsonSerializer.Deserialize<UserSession>(sessionJson, _serializerOptions);
                _hasLoadedFromCookie = true;
                OnChange?.Invoke();
            }
            else
            {
                _hasLoadedFromCookie = true;
            }
        }
        catch (JSException)
        {
            // Ignore parsing/interop errors, caller can attempt again later.
        }
        catch (JsonException)
        {
            CurrentSession = null;
            _hasLoadedFromCookie = true;
        }

        return CurrentSession;
    }

    public async Task<UserSession?> LoginAsync(string identifier, string password, bool rememberMe)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        var account = await _authRepository.AuthenticateAsync(identifier.Trim(), password);
        if (account is null)
        {
            return null;
        }

        var person = await _personRepository.GetByIdAsync(account.PersonId)
            ?? await _personRepository.GetByAccountIdAsync(account.Id);

        CurrentSession = new UserSession
        {
            AccountId = account.Id,
            PersonId = account.PersonId,
            Username = account.Username,
            DisplayName = person?.FullName ?? account.Username,
            Email = person?.Email,
            Phone = person?.Phone,
            RememberMe = rememberMe,
            LoginTime = DateTimeOffset.UtcNow
        };

        var jsonPayload = JsonSerializer.Serialize(CurrentSession, _serializerOptions);
        if (await TryEnsureModuleAsync())
        {
            var durationInDays = rememberMe ? RememberMeDays : DefaultSessionDays;
            await _authModule!.InvokeVoidAsync("setUserCookie", jsonPayload, durationInDays);
        }

        _hasLoadedFromCookie = true;
        OnChange?.Invoke();
        return CurrentSession;
    }

    public async Task LogoutAsync()
    {
        CurrentSession = null;
        _hasLoadedFromCookie = false;

        if (await TryEnsureModuleAsync())
        {
            await _authModule!.InvokeVoidAsync("clearUserCookie");
        }

        OnChange?.Invoke();
    }

    private async Task<bool> TryEnsureModuleAsync()
    {
        if (_authModule is not null)
        {
            return true;
        }

        try
        {
            _authModule = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", AuthModulePath);
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("prerender", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
        catch (JSException)
        {
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_authModule is not null)
        {
            try
            {
                await _authModule.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors.
            }
        }
    }
}
