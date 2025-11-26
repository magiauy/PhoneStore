using Microsoft.AspNetCore.Components.Forms;

namespace PhoneStoreUser.Services;

public interface ICloudinaryService
{
    Task<string?> UploadImageAsync(IBrowserFile file, string folder, string publicId);
    Task<bool> DeleteImageAsync(string publicId);
    string? BuildPublicId(string brand, string slug, string sku);
    Task<string?> GetImageUrlAsync(string brand, string slug, string sku, string? cacheBuster = null, string? format = "webp", string? transformation = null);
}
