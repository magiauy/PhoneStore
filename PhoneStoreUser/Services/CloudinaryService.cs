using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Components.Forms;

namespace PhoneStoreUser.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string> UploadImageAsync(IBrowserFile file, string folder, string publicId)
    {
        if (_cloudinary == null) return null;

        try
        {
            var stream = file.OpenReadStream(10 * 1024 * 1024); // 10MB max
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.Name, stream),
                Folder = folder,
                PublicId = publicId,
                Overwrite = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            return uploadResult.SecureUrl.ToString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary upload failed: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (_cloudinary == null) return false;

        try
        {
            var deletionParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deletionParams);
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary delete failed: {ex.Message}");
            return false;
        }
    }
}
