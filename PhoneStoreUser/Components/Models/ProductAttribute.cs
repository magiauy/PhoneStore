namespace PhoneStoreUser.Components.Models;

public record ProductAttribute(
    int Id,
    string Name,
    string DataType,
    string? Note = null
);
