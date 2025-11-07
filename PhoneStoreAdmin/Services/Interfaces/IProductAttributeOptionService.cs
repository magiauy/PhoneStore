using System.Collections.Generic;
using PhoneStoreAdmin.Models;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IProductAttributeOptionService
    {
        IReadOnlyList<ProductAttributeOption> GetOptionsForAttribute(int attributeId);
        IDictionary<int, IReadOnlyList<ProductAttributeOption>> GetOptionsForAttributes(IEnumerable<int> attributeIds);
        ProductAttributeOption? GetOptionById(int optionId);
        ProductAttributeOption? FindByDisplay(int attributeId, string displayValue);
        void EnsureOption(int attributeId, string displayValue, string? normalizedValue, int sortOrder, bool isActive = true);
    }
}
