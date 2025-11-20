using System.Collections.Generic;
using PhoneStoreRepository.Models;
using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductModel>> GetProductModelsAsync();
    Task<ProductModel?> GetProductModelBySlugAsync(string slug);
    Task<IReadOnlyList<ProductModel>> GetProductModelsByCategorySlugAsync(string categorySlug);
    Task<IReadOnlyList<ProductVariantViewModel>> GetProductsByModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttributeValue>> GetAttributeValuesForProductsAsync(IEnumerable<int> productIds);
    Task<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>> GetAttributeOptionsForAttributesAsync(IEnumerable<int> attributeIds);
}
