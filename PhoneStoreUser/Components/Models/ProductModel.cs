namespace PhoneStoreUser.Components.Models;

public record ProductModel(
    int Id,
    string Name,
    string Slug,
    string? Description = null,
    string? Image = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
