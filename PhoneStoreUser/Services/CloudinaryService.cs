using System.Collections.Concurrent;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Components.Forms;

namespace PhoneStoreUser.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly string? _cloudName;
    private readonly ConcurrentDictionary<string, string> _versionCache = new();
    private const string BaseFolder = "mobileguzone";

    public CloudinaryService(IConfiguration config)
    {
        _cloudName = config["Cloudinary:CloudName"];
        var apiKey = config["Cloudinary:ApiKey"];
        var apiSecret = config["Cloudinary:ApiSecret"];

        if (!string.IsNullOrEmpty(_cloudName) && !string.IsNullOrEmpty(apiKey) && !string.IsNullOrEmpty(apiSecret))
        {
            var account = new Account(_cloudName, apiKey, apiSecret);
            _cloudinary = new Cloudinary(account);
        }
    }

    public async Task<string?> UploadImageAsync(IBrowserFile file, string folder, string publicId)
    {
        if (_cloudinary == null) return null;

        try
        {
            await using var stream = file.OpenReadStream(10 * 1024 * 1024); // 10MB max
            var publicIdWithFolder = CombineFolderAndPublicId(folder, publicId);
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.Name, stream),
                PublicId = publicIdWithFolder,
                Overwrite = true,
                Invalidate = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            if (!string.IsNullOrEmpty(uploadResult?.PublicId) && !string.IsNullOrEmpty(uploadResult.Version))
            {
                _versionCache[uploadResult.PublicId] = uploadResult.Version;
            }
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
            var deletionParams = new DeletionParams(publicId)
            {
                Invalidate = true
            };
            Console.WriteLine($"Attempting to delete Cloudinary image with PublicId: {deletionParams.PublicId}");
            var result = await _cloudinary.DestroyAsync(deletionParams);
            Console.WriteLine($"Cloudinary delete result: {result.Result}");
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary delete failed: {ex.Message}");
            return false;
        }
    }

    public string? BuildPublicId(string brand, string slug, string sku)
    {
        var brandSegment = NormalizeSegment(brand, toLower: true);
        var slugSegment = NormalizeSegment(slug, toLower: false);
        var skuSegment = NormalizeSegment(sku, toLower: false);

        if (brandSegment == null || slugSegment == null || skuSegment == null)
        {
            return null;
        }

        return $"{BaseFolder}/{brandSegment}/{slugSegment}/{skuSegment}";
    }

    public async Task<string?> GetImageUrlAsync(
        string brand,
        string slug,
        string sku,
        string? cacheBuster = null,
        string? format = "webp",
        string? transformation = null)
    {
        var publicId = BuildPublicId(brand, slug, sku);
        if (publicId == null)
        {
            return null;
        }

        var version = await GetVersionAsync(publicId);
        return BuildUrl(publicId, version, cacheBuster, format, transformation);
    }

    private async Task<string?> GetVersionAsync(string publicId)
    {
        if (_versionCache.TryGetValue(publicId, out var cachedVersion))
        {
            return cachedVersion;
        }

        if (_cloudinary == null)
        {
            return null;
        }

        try
        {
            var resource = await _cloudinary.GetResourceAsync(new GetResourceParams(publicId));
            var version = resource?.Version;
            if (!string.IsNullOrEmpty(version))
            {
                _versionCache[publicId] = version;
            }
            return version;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cloudinary get version failed for {publicId}: {ex.Message}");
            return null;
        }
    }

    private string? BuildUrl(string publicId, string? version, string? cacheBuster, string? format, string? transformation)
    {
        var builder = _cloudinary?.Api.UrlImgUp.Secure(true);

        if (builder == null)
        {
            if (string.IsNullOrEmpty(_cloudName))
            {
                return null;
            }

            var versionSegment = string.IsNullOrEmpty(version) ? string.Empty : $"v{version}/";
            var formatSuffix = string.IsNullOrEmpty(format) ? string.Empty : $".{format}";
            var url = $"https://res.cloudinary.com/{_cloudName}/image/upload/{versionSegment}{publicId}{formatSuffix}";
            return AppendCacheBuster(url, cacheBuster, version);
        }

        if (!string.IsNullOrWhiteSpace(format))
        {
            builder = builder.Format(format);
        }

        if (!string.IsNullOrWhiteSpace(transformation))
        {
            builder = builder.Transform(new Transformation().RawTransformation(transformation));
        }

        if (!string.IsNullOrWhiteSpace(version))
        {
            builder = builder.Version(version);
        }

        var builtUrl = builder.BuildUrl(publicId);
        return AppendCacheBuster(builtUrl, cacheBuster, version);
    }

    private static string AppendCacheBuster(string url, string? cacheBuster, string? version)
    {
        if (!string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(cacheBuster))
        {
            return url;
        }

        var separator = url.Contains("?") ? "&" : "?";
        return $"{url}{separator}v={Uri.EscapeDataString(cacheBuster)}";
    }

    private static string CombineFolderAndPublicId(string folder, string publicId)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return publicId.Trim('/');
        }

        return $"{folder.TrimEnd('/')}/{publicId.TrimStart('/')}";
    }

    private static string? NormalizeSegment(string value, bool toLower)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var segment = value.Trim();
        segment = toLower ? segment.ToLowerInvariant() : segment;
        return Uri.EscapeDataString(segment);
    }
}
