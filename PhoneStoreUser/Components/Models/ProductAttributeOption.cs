using System;

namespace PhoneStoreUser.Components.Models;

public record ProductAttributeOption(
    int Id,
    int AttributeId,
    string DisplayValue,
    string? NormalizedValue = null,
    int SortOrder = 0,
    bool IsActive = true,
    DateTime? CreatedAt = null,
    DateTime? UpdatedAt = null
);
