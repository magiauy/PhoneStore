namespace PhoneStoreUser.Components.Models;

public record BrandBadgeModel(
    string Name,
    string Tagline,
    string Initial,
    string? LogoUrl = null);
