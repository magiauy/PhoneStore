using Microsoft.AspNetCore.Components;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Services;
using System;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    [Inject]
    public IUserSessionService? SessionService { get; set; }

    public LoginModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool ShowSuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    private bool _hasRedirectedFromSession;

    protected override async Task OnInitializedAsync()
    {
        await RedirectIfAuthenticatedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RedirectIfAuthenticatedAsync();
        }
    }

    private async Task HandleValidSubmit()
    {
        if (SessionService is null)
        {
            return;
        }

        IsLoading = true;
        ErrorMessage = null;
        ShowSuccessMessage = false;

        try
        {
            var session = await SessionService.LoginAsync(
                Model.EmailOrUsername.Trim(),
                Model.Password,
                Model.RememberMe);

            if (session is null)
            {
                ErrorMessage = "Sai tên đăng nhập hoặc mật khẩu.";
                return;
            }

            ShowSuccessMessage = true;
            StateHasChanged();

            await Task.Delay(1500);
            NavigationManager?.NavigateTo("/");
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

    private async Task RedirectIfAuthenticatedAsync()
    {
        if (_hasRedirectedFromSession || SessionService is null || NavigationManager is null)
        {
            return;
        }

        var session = SessionService.CurrentSession ?? await SessionService.InitializeAsync();
        if (session is not null)
        {
            _hasRedirectedFromSession = true;
            NavigationManager.NavigateTo("/", true);
        }
    }
}
