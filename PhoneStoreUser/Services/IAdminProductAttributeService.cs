using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IAdminProductAttributeService
{
    // Product Attributes
    Task<PagedResult<ProductAttributeEntity>> GetAttributesAsync(string? search, string? dataType, int page = 1, int pageSize = 10);
    Task<ProductAttributeEntity?> GetAttributeByIdAsync(int id);
    Task<bool> CreateAttributeAsync(ProductAttributeEntity attribute);
    Task<bool> UpdateAttributeAsync(ProductAttributeEntity attribute);
    Task<bool> DeleteAttributeAsync(int id);
    Task<bool> IsAttributeNameExistsAsync(string name, int? excludeId = null);

    // Attribute Options
    Task<List<ProductAttributeOptionEntity>> GetOptionsByAttributeIdAsync(int attributeId);
    Task<ProductAttributeOptionEntity?> GetOptionByIdAsync(int id);
    Task<bool> CreateOptionAsync(ProductAttributeOptionEntity option);
    Task<bool> UpdateOptionAsync(ProductAttributeOptionEntity option);
    Task<bool> DeleteOptionAsync(int id);

    // Attribute Values (for products)
    Task<List<ProductAttributeValueEntity>> GetValuesByProductIdAsync(int productId);
    Task<bool> SaveProductAttributeValuesAsync(int productId, List<ProductAttributeValueEntity> values);
}
