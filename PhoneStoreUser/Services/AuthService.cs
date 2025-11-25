using System;
using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;
using PhoneStoreUser.Utils;

namespace PhoneStoreUser.Services;

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AuthService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task ChangePasswordAsync(int accountId, ChangePasswordModel model)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        using var context = await _dbContextFactory.CreateDbContextAsync();
        var account = await context.Accounts.FindAsync(accountId);
        if (account == null)
        {
            throw new InvalidOperationException("Account not found.");
        }

        if (!PasswordHasher.VerifyPassword(model.OldPassword, account.Password))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        account.Password = PasswordHasher.HashPassword(model.NewPassword);
        await context.SaveChangesAsync();
    }
}
