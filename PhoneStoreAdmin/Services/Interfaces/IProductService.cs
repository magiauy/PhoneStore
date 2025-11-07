using System.Collections.Generic;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.ViewModels;

namespace PhoneStoreAdmin.Services.Interfaces
{
    /// <summary>
    /// Provides business operations related to product management.
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Search products with pagination by sku, name, category, brand, and status filters.
        /// </summary>
        /// <param name="sku">Optional SKU filter.</param>
        /// <param name="name">Optional name filter.</param>
        /// <param name="categoryId">Optional category identifier filter.</param>
        /// <param name="brandId">Optional brand identifier filter.</param>
        /// <param name="status">Optional product status filter.</param>
        /// <param name="page">Requested page, defaults to 1.</param>
        /// <param name="pageSize">Page size, defaults to 20.</param>
        /// <returns>Paged list of products for the UI.</returns>
        ProductSearchResult SearchProducts(
            string? sku,
            string? name,
            int? categoryId,
            int? brandId,
            ProductStatus? status,
            int page = 1,
            int pageSize = 20);

        /// <summary>
        /// Search products with a filter criteria object.
        /// </summary>
        /// <param name="criteria">Filter options for the query.</param>
        /// <returns>Paged list of products for the UI.</returns>
        ProductSearchResult SearchProducts(ProductFilterCriteria criteria);

        /// <summary>
        /// Retrieve all product models for selection lists.
        /// </summary>
        IReadOnlyList<ProductModel> GetAllModels();

        /// <summary>
        /// Retrieve product attribute definitions including available options.
        /// </summary>
        IReadOnlyList<ProductAttributeDefinition> GetAttributeDefinitions();

        /// <summary>
        /// Retrieve product detail with attribute values and serials.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>Detail view model or null when product not found.</returns>
        ProductDetailViewModel? GetProductDetail(int productId);

        /// <summary>
        /// Create a new product and optionally persist dynamic attribute values.
        /// </summary>
        /// <param name="product">Product entity to create.</param>
        /// <param name="attributeValues">Optional dynamic attribute values.</param>
        /// <returns>True when creation succeeds.</returns>
        bool CreateProduct(Product product, IEnumerable<ProductAttributeValueInput>? attributeValues = null);

        /// <summary>
        /// Update an existing product and optionally persist dynamic attribute values.
        /// </summary>
        /// <param name="product">Product entity to update.</param>
        /// <param name="attributeValues">Optional dynamic attribute values.</param>
        /// <returns>True when update succeeds.</returns>
        bool UpdateProduct(Product product, IEnumerable<ProductAttributeValueInput>? attributeValues = null);

        /// <summary>
        /// Delete a product and its attribute values and serials.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <returns>True when deletion succeeds.</returns>
        bool DeleteProduct(int productId);

        /// <summary>
        /// Save dynamic attribute values for the product.
        /// </summary>
        /// <param name="productId">Product identifier.</param>
        /// <param name="attributeValues">Attribute values to persist.</param>
        /// <returns>True when persistence succeeds.</returns>
        bool SaveAttributeValues(int productId, IEnumerable<ProductAttributeValueInput> attributeValues);
    }
}
