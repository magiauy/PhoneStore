using Microsoft.AspNetCore.Components;
using PhoneStoreUser.Components.Models;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Login : ComponentBase
{
    public LoginModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    private async Task HandleValidSubmit()
    {
        IsLoading = true;

        try
        {
            await Task.Delay(1000);
            // TODO: Replace with real authentication API call.
        }
        finally
        {
            IsLoading = false;
        }
    }
}
