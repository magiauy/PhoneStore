namespace PhoneStoreUser.Components.Models;

public record ProductAttribute(
    int Id,
    string Code,
    string Name,
    string Type,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
