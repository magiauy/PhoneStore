using Microsoft.AspNetCore.Components;
using PhoneStoreUser.Components.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Register : ComponentBase
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    public RegisterModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool ShowSuccessMessage { get; set; }

    private async Task HandleSubmit()
    {
        var validationContext = new ValidationContext(Model);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(Model, validationContext, validationResults, true);

        if (!isValid)
        {
            return;
        }

        await HandleValidSubmit();
    }

    private async Task HandleValidSubmit()
    {
        IsLoading = true;
        ShowSuccessMessage = false;

        try
        {
            await Task.Delay(1200);
            ShowSuccessMessage = true;
            StateHasChanged();
            await Task.Delay(2500);
            NavigationManager?.NavigateTo("/login", forceLoad: true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
