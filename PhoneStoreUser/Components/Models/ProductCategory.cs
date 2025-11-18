namespace PhoneStoreUser.Components.Models;

public record ProductCategory(
    int Id,
    string Name,
    int? ParentId = null,
    string? Note = null
);
