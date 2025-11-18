using System.Collections.Generic;
using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductModel>> GetProductModelsAsync();
    Task<ProductModel?> GetProductModelBySlugAsync(string slug);
    Task<IReadOnlyList<Product>> GetProductsByModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttributeValue>> GetAttributeValuesForProductsAsync(IEnumerable<int> productIds);
    Task<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>> GetAttributeOptionsForAttributesAsync(IEnumerable<int> attributeIds);
}
