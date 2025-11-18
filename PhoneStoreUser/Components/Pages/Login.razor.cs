using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using PhoneStoreUser.Components.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Login : ComponentBase
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    public LoginModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool ShowSuccessMessage { get; set; }

    private async Task HandleSubmit()
    {
        Console.WriteLine($"[Login] HandleSubmit called with EmailOrUsername: {Model.EmailOrUsername}");
        
        // Validate manually
        var validationContext = new ValidationContext(Model);
        var validationResults = new List<ValidationResult>();
        bool isValid = Validator.TryValidateObject(Model, validationContext, validationResults, true);

        if (!isValid)
        {
            Console.WriteLine($"[Login] Validation failed with {validationResults.Count} errors");
            foreach (var error in validationResults)
            {
                Console.WriteLine($"[Login] - {error.ErrorMessage}");
            }
            return;
        }

        Console.WriteLine($"[Login] Validation passed, calling HandleValidSubmit");
        await HandleValidSubmit();
    }

    private async Task HandleValidSubmit()
    {
        Console.WriteLine("[Login] HandleValidSubmit - Starting login process");
        IsLoading = true;
        ShowSuccessMessage = false;

        try
        {
            await Task.Delay(1000);
            Console.WriteLine("[Login] Simulated API call completed");
            // TODO: Replace with real authentication API call.
            // For now, always login success
            
            // Show success message
            ShowSuccessMessage = true;
            Console.WriteLine("[Login] Success message displayed");
            StateHasChanged(); // Force re-render để hiển thị toast
            
            // Navigate after showing message and animation (2.8s for animation to complete)
            await Task.Delay(2800);
            Console.WriteLine("[Login] Navigating to home page");
            NavigationManager?.NavigateTo("/", forceLoad: true);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
