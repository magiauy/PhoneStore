using System.Collections.Generic;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Components.ViewModels;

namespace PhoneStoreUser.Services;

public interface IProductCatalogService
{
    Task<IReadOnlyList<ProductModel>> GetProductModelsAsync();
    Task<ProductModel?> GetProductModelBySlugAsync(string slug);
    Task<IReadOnlyList<ProductModel>> GetProductModelsByCategorySlugAsync(string categorySlug);
    Task<IReadOnlyList<Product>> GetProductsByModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId);
    Task<IReadOnlyList<ProductAttributeValue>> GetAttributeValuesForProductsAsync(IEnumerable<int> productIds);
    Task<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>> GetAttributeOptionsForAttributesAsync(IEnumerable<int> attributeIds);
    Task<IReadOnlyList<Brand>> GetBrandsAsync();
    Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync();
    Task<IReadOnlyList<ProductModel>> GetFilteredProductModelsAsync(IEnumerable<int>? brandIds = null, IEnumerable<int>? categoryIds = null);
    Task<IReadOnlyList<ProductCardViewModel>> GetFilteredProductsAsync(string? searchTerm = null, IEnumerable<int>? brandIds = null, IEnumerable<int>? categoryIds = null);
    Task<Product?> GetProductBySkuAsync(string sku);
    Task<ProductModel?> GetProductModelByIdAsync(int id);
}
