namespace PhoneStore.Services.Interfaces;

/// <summary>
/// Service interface for Cloudinary image management operations
/// </summary>
public interface ICloudinaryService
{
    /// <summary>
    /// Upload an image to Cloudinary
    /// </summary>
    /// <param name="stream">Image stream to upload</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="publicId">Full public ID for the image (including folder path)</param>
    /// <returns>Secure URL of the uploaded image, or null if failed</returns>
    Task<string?> UploadImageAsync(Stream stream, string fileName, string publicId);

    /// <summary>
    /// Delete an image from Cloudinary
    /// </summary>
    /// <param name="publicId">Public ID of the image to delete</param>
    /// <returns>True if deletion was successful</returns>
    Task<bool> DeleteImageAsync(string publicId);

    /// <summary>
    /// Build a public ID for a product image based on brand, slug, and SKU
    /// </summary>
    /// <param name="brand">Brand name</param>
    /// <param name="slug">Product slug</param>
    /// <param name="sku">Product SKU</param>
    /// <returns>Formatted public ID, or null if any parameter is invalid</returns>
    string? BuildPublicId(string brand, string slug, string sku);

    /// <summary>
    /// Build a public ID for a product model image
    /// </summary>
    /// <param name="modelSlug">Product model slug</param>
    /// <returns>Formatted public ID, or null if parameter is invalid</returns>
    string? BuildModelPublicId(string modelSlug);

    /// <summary>
    /// Build the Cloudinary URL for an image
    /// </summary>
    /// <param name="publicId">Public ID of the image</param>
    /// <param name="format">Image format (default: webp)</param>
    /// <param name="transformation">Optional transformation string</param>
    /// <returns>Complete Cloudinary URL, or null if cloudName is not configured</returns>
    string? BuildImageUrl(string publicId, string? format = "webp", string? transformation = null);

    /// <summary>
    /// Get the image URL for a product based on brand, slug, and SKU
    /// </summary>
    /// <param name="brand">Brand name</param>
    /// <param name="slug">Product slug</param>
    /// <param name="sku">Product SKU</param>
    /// <param name="format">Image format (default: webp)</param>
    /// <param name="transformation">Optional transformation string</param>
    /// <returns>Complete Cloudinary URL, or null if parameters are invalid</returns>
    string? GetProductImageUrl(string brand, string slug, string sku, string? format = "webp", string? transformation = null);

    /// <summary>
    /// Get the image URL for a product model
    /// </summary>
    /// <param name="modelSlug">Product model slug</param>
    /// <param name="format">Image format (default: webp)</param>
    /// <param name="transformation">Optional transformation string</param>
    /// <returns>Complete Cloudinary URL, or null if parameter is invalid</returns>
    string? GetModelImageUrl(string modelSlug, string? format = "webp", string? transformation = null);
}
