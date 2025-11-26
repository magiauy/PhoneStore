using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IProductService
{
    Task<PagedResult<ProductEntity>> GetProductsAsync(string? search, int page = 1, int pageSize = 10);
    Task<ProductEntity?> GetProductByIdAsync(int id);
    Task<bool> CreateProductAsync(ProductEntity product);
    Task<bool> UpdateProductAsync(ProductEntity product);
    Task<bool> DeleteProductAsync(int id);
    Task<List<ProductEntity>> GetAllProductsAsync();

    // New methods for Product Model and Attributes
    Task<List<ProductModelEntity>> GetProductModelsAsync();
    Task<List<ProductAttributeEntity>> GetAttributesByModelIdAsync(int modelId);
    Task<List<ProductAttributeOptionEntity>> GetAttributeOptionsAsync(int attributeId);
    Task<List<ProductAttributeValueEntity>> GetProductAttributeValuesAsync(int productId);
    Task SaveProductAttributeValuesAsync(int productId, List<ProductAttributeValueEntity> values);
}
