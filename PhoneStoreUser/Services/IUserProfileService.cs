using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface IUserProfileService
{
    Task<ProfileModel?> GetProfileAsync(int personId);
    Task UpdateProfileAsync(ProfileModel model);
}
