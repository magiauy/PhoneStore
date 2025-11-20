namespace PhoneStoreUser.Components.ViewModels;

public record BrandBadgeModel(
    string Name,
    string Tagline,
    string Initial,
    string? LogoUrl = null);
