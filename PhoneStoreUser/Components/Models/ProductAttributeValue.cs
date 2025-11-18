namespace PhoneStoreUser.Components.Models;

public record ProductAttributeValue(
    int Id,
    int ProductId,
    int AttributeId,
    int? OptionId = null,
    string? ValueText = null,
    decimal? ValueNumber = null,
    DateTime? ValueDate = null,
    bool? ValueBool = null
);
