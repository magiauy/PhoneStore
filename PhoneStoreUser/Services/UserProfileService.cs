using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class UserProfileService : IUserProfileService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public UserProfileService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<ProfileModel?> GetProfileAsync(int personId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var person = await context.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personId);

        if (person == null) return null;

        var customer = await context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.PersonId == personId);

        return new ProfileModel
        {
            PersonId = person.Id,
            FullName = person.FullName,
            Email = person.Email,
            Phone = person.Phone,
            Address = customer?.Address
        };
    }

    public async Task UpdateProfileAsync(ProfileModel model)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var person = await context.People.FindAsync(model.PersonId);
        if (person == null) return;

        person.FullName = model.FullName;
        person.Email = model.Email;
        person.Phone = model.Phone;

        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.PersonId == model.PersonId);

        if (customer == null)
        {
            // If customer record doesn't exist but address is provided, create it?
            // Or maybe every person is a customer?
            // The user said "khách hàng đăng nhập vào sẽ có một thuộc tính từ bảng customer".
            // So likely the customer record exists.
            // But if not, we should probably create it if address is set.
            // For now, let's assume we create it if it's missing.
            customer = new CustomerEntity
            {
                PersonId = model.PersonId,
                Address = model.Address
            };
            context.Customers.Add(customer);
        }
        else
        {
            customer.Address = model.Address;
        }

        await context.SaveChangesAsync();
    }
}
