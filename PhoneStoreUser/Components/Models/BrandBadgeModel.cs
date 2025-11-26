namespace PhoneStoreUser.Components.Models;

public record BrandBadgeModel(
    int Id,
    string Name,
    string Tagline,
    string Initial,
    string? LogoUrl = null);
