using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using PhoneStoreUser.Components.Models;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    [Inject]
    public AuthenticationStateProvider? AuthenticationStateProvider { get; set; }

    [Inject]
    public IHttpClientFactory? HttpClientFactory { get; set; }

    [Parameter, SupplyParameterFromQuery]
    public string? ReturnUrl { get; set; }

    public LoginModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool ShowSuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    private HttpClient? _httpClient;
    private bool _isCheckingAuth = true;

    protected override async Task OnInitializedAsync()
    {
        if (NavigationManager is not null)
        {
            _httpClient = HttpClientFactory?.CreateClient();
            if (_httpClient is not null)
            {
                _httpClient.BaseAddress = new Uri(NavigationManager.BaseUri);
            }

            var uri = NavigationManager.ToAbsoluteUri(NavigationManager.Uri);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
            if (query.TryGetValue("error", out var error))
            {
                var errorValue = error.ToString();
                ErrorMessage = errorValue switch
                {
                    "missing_credentials" => "Vui lòng nhập đầy đủ thông tin.",
                    "invalid_credentials" => "Sai tên đăng nhập hoặc mật khẩu.",
                    _ => "Đăng nhập thất bại."
                };
            }

            if (query.TryGetValue("success", out var successValue))
            {
                var successString = successValue.ToString();
                if (successString.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                    successString.Equals("1", StringComparison.OrdinalIgnoreCase))
                {
                    ShowSuccessMessage = true;
                }
            }
        }

        await CheckAuthStatusAsync();
    }

    private async Task CheckAuthStatusAsync()
    {
        if (AuthenticationStateProvider is null || NavigationManager is null)
        {
            _isCheckingAuth = false;
            return;
        }

        var authState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            NavigationManager.NavigateTo(ReturnUrl ?? "/", true);
            return;
        }

        _isCheckingAuth = false;
    }
    private async Task HandleValidSubmit()
    {
        IsLoading = true;
        ErrorMessage = null;
        ShowSuccessMessage = false;

        try
        {
            if (_httpClient is null)
            {
                ErrorMessage = "Không thể kết nối máy chủ.";
                return;
            }

            var response = await _httpClient.PostAsJsonAsync("login", Model);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                ErrorMessage = "Không thể đăng nhập ngay lúc này. Vui lòng thử lại sau.";
                return;
            }

            ShowSuccessMessage = true;
            StateHasChanged();

            await Task.Delay(1500);
            NavigationManager?.NavigateTo(ReturnUrl ?? "/", true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Login] Login failed: {ex.Message}");
            ErrorMessage = "Không thể đăng nhập ngay lúc này. Vui lòng thử lại sau.";
        }
        finally
        {
            IsLoading = false;
        }
    }

}
