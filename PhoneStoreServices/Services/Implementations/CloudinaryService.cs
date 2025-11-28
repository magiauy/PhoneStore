using System.Collections.Concurrent;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Configuration;
using PhoneStore.Services.Interfaces;

namespace PhoneStore.Services.Implementations;

/// <summary>
/// Cloudinary service implementation for image management
/// </summary>
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary? _cloudinary;
    private readonly string? _cloudName;
    private readonly ConcurrentDictionary<string, string> _versionCache = new();
    private const string BaseFolder = "mobileguzone";
    private const string ModelsFolder = "models";
    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB max

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

    /// <inheritdoc />
    public async Task<string?> UploadImageAsync(Stream stream, string fileName, string publicId)
    {
        if (_cloudinary == null) return null;

        try
        {
            System.Diagnostics.Debug.WriteLine($"Cloudinary upload: publicId={publicId}");
            
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(fileName, stream),
                PublicId = publicId,
                Overwrite = true,
                Invalidate = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);
            System.Diagnostics.Debug.WriteLine($"Cloudinary upload result: PublicId={uploadResult?.PublicId}, SecureUrl={uploadResult?.SecureUrl}");
            
            if (!string.IsNullOrEmpty(uploadResult?.PublicId) && !string.IsNullOrEmpty(uploadResult.Version))
            {
                _versionCache[uploadResult.PublicId] = uploadResult.Version;
            }
            return uploadResult?.SecureUrl?.ToString();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cloudinary upload failed: {ex.Message}");
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteImageAsync(string publicId)
    {
        if (_cloudinary == null) return false;

        try
        {
            var deletionParams = new DeletionParams(publicId)
            {
                Invalidate = true
            };
            System.Diagnostics.Debug.WriteLine($"Attempting to delete Cloudinary image with PublicId: {deletionParams.PublicId}");
            var result = await _cloudinary.DestroyAsync(deletionParams);
            System.Diagnostics.Debug.WriteLine($"Cloudinary delete result: {result.Result}");
            return result.Result == "ok";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Cloudinary delete failed: {ex.Message}");
            return false;
        }
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public string? BuildModelPublicId(string modelSlug)
    {
        var slugSegment = NormalizeSegment(modelSlug, toLower: false);
        if (slugSegment == null)
        {
            return null;
        }

        return $"{BaseFolder}/{ModelsFolder}/{slugSegment}";
    }

    /// <inheritdoc />
    public string? BuildImageUrl(string publicId, string? format = "webp", string? transformation = null)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            return null;
        }

        return BuildUrl(publicId, version: null, cacheBuster: null, format, transformation);
    }

    /// <inheritdoc />
    public string? GetProductImageUrl(string brand, string slug, string sku, string? format = "webp", string? transformation = null)
    {
        var publicId = BuildPublicId(brand, slug, sku);
        if (publicId == null)
        {
            return null;
        }

        return BuildUrl(publicId, version: null, cacheBuster: null, format, transformation);
    }

    /// <inheritdoc />
    public string? GetModelImageUrl(string modelSlug, string? format = "webp", string? transformation = null)
    {
        var publicId = BuildModelPublicId(modelSlug);
        if (publicId == null)
        {
            return null;
        }

        return BuildUrl(publicId, version: null, cacheBuster: null, format, transformation);
    }

    /// <summary>
    /// Build the complete Cloudinary URL for an image
    /// </summary>
    private string? BuildUrl(string publicId, string? version, string? cacheBuster, string? format, string? transformation)
    {
        var builder = _cloudinary?.Api.UrlImgUp.Secure(true);

        if (builder == null)
        {
            // Fallback: build URL manually if Cloudinary client is not available
            if (string.IsNullOrEmpty(_cloudName))
            {
                return null;
            }

            var versionSegment = string.IsNullOrEmpty(version) ? string.Empty : $"v{version}/";
            var formatSuffix = string.IsNullOrEmpty(format) ? string.Empty : $".{format}";
            var transformationSegment = string.IsNullOrEmpty(transformation) ? string.Empty : $"{transformation}/";
            var url = $"https://res.cloudinary.com/{_cloudName}/image/upload/{transformationSegment}{versionSegment}{publicId}{formatSuffix}";
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

    /// <summary>
    /// Append cache buster parameter to URL if needed
    /// </summary>
    private static string AppendCacheBuster(string url, string? cacheBuster, string? version)
    {
        if (!string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(cacheBuster))
        {
            return url;
        }

        var separator = url.Contains('?') ? "&" : "?";
        return $"{url}{separator}v={Uri.EscapeDataString(cacheBuster)}";
    }

    /// <summary>
    /// Combine folder path with public ID
    /// </summary>
    private static string CombineFolderAndPublicId(string folder, string publicId)
    {
        if (string.IsNullOrWhiteSpace(folder))
        {
            return publicId.Trim('/');
        }

        return $"{folder.TrimEnd('/')}/{publicId.TrimStart('/')}";
    }

    /// <summary>
    /// Normalize a path segment for use in public ID
    /// </summary>
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
