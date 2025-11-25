using Microsoft.AspNetCore.Components.Forms;

namespace PhoneStoreUser.Services;

public interface ICloudinaryService
{
    Task<string> UploadImageAsync(IBrowserFile file, string folder, string publicId);
    Task<bool> DeleteImageAsync(string publicId);
}
