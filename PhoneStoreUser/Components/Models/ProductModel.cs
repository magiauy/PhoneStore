namespace PhoneStoreUser.Components.Models;

public record ProductModel(
    int Id,
    string Name,
    string Slug,
    string Description,
    string? Image = null,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
