using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;
using PhoneStoreUser.Utils;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace PhoneStoreUser.Components.Pages;

public partial class Register : ComponentBase
{
    [Inject]
    public NavigationManager? NavigationManager { get; set; }

    [Inject]
    public IDbContextFactory<AppDbContext>? DbContextFactory { get; set; }

    public RegisterModel Model { get; set; } = new();

    public bool IsLoading { get; set; }

    public bool ShowSuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    private async Task HandleSubmit()
    {
        // Clear previous error message
        ErrorMessage = null;

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
        if (DbContextFactory is null)
        {
            ErrorMessage = "Lỗi hệ thống. Vui lòng thử lại sau.";
            return;
        }

        IsLoading = true;
        ShowSuccessMessage = false;
        ErrorMessage = null;

        try
        {
            await using var dbContext = await DbContextFactory.CreateDbContextAsync();

            // Check for duplicate username
            var existingUsername = await dbContext.Accounts
                .AnyAsync(a => a.Username == Model.Username.Trim());
            if (existingUsername)
            {
                ErrorMessage = "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.";
                return;
            }

            // Check for duplicate email
            var existingEmail = await dbContext.Persons
                .AnyAsync(p => p.Email == Model.Email.Trim());
            if (existingEmail)
            {
                ErrorMessage = "Email đã được sử dụng. Vui lòng sử dụng email khác.";
                return;
            }

            // Generate a unique code for the person
            var personCode = $"CUS{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";

            // Create Person entity
            var person = new PersonEntity
            {
                Code = personCode,
                FullName = Model.FullName.Trim(),
                Email = Model.Email.Trim(),
                Phone = Model.Phone.Trim(),
                PersonType = "CUSTOMER",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            dbContext.Persons.Add(person);
            await dbContext.SaveChangesAsync();

            // Create Customer entity
            var customer = new CustomerEntity
            {
                PersonId = person.Id,
                Address = string.Empty
            };

            dbContext.Customers.Add(customer);

            // Hash password and create Account entity
            var hashedPassword = PasswordHasher.HashPassword(Model.Password);
            var account = new AccountEntity
            {
                Username = Model.Username.Trim(),
                Password = hashedPassword,
                PersonId = person.Id,
                IsActive = true,
                LastLogin = null
            };

            dbContext.Accounts.Add(account);
            await dbContext.SaveChangesAsync();

            // Show success message and redirect
            ShowSuccessMessage = true;
            StateHasChanged();
            await Task.Delay(2500);
            NavigationManager?.NavigateTo("/login?registered=true", forceLoad: true);
        }
        catch (Exception ex)
        {
            ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.";
            Console.WriteLine($"Registration error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
