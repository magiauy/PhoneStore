using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreUser.Components.Models;

namespace PhoneStoreUser.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly IProductModelRepository _productModelRepository;
    private readonly IProductRepository _productRepository;
    private readonly IProductModelAttributeRepository _productModelAttributeRepository;
    private readonly IProductAttributeRepository _productAttributeRepository;
    private readonly IProductAttributeValueRepository _productAttributeValueRepository;
    private readonly IProductAttributeOptionRepository _productAttributeOptionRepository;
    private readonly ILogger<ProductCatalogService> _logger;

    public ProductCatalogService(
        IProductModelRepository productModelRepository,
        IProductRepository productRepository,
        IProductModelAttributeRepository productModelAttributeRepository,
        IProductAttributeRepository productAttributeRepository,
        IProductAttributeValueRepository productAttributeValueRepository,
        IProductAttributeOptionRepository productAttributeOptionRepository,
        ILogger<ProductCatalogService> logger)
    {
        _productModelRepository = productModelRepository;
        _productRepository = productRepository;
        _productModelAttributeRepository = productModelAttributeRepository;
        _productAttributeRepository = productAttributeRepository;
        _productAttributeValueRepository = productAttributeValueRepository;
        _productAttributeOptionRepository = productAttributeOptionRepository;
        _logger = logger;
    }

    public Task<IReadOnlyList<ProductModel>> GetProductModelsAsync()
    {
        return ExecuteAsync(() =>
        {
            var models = _productModelRepository.GetAll() ?? Enumerable.Empty<PhoneStoreRepository.Models.ProductModel>();
            return models
                .Select(MapProductModel)
                .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
                .ThenBy(m => m.Name)
                .ToList()
                .AsReadOnly();
        }, "load product models", Array.Empty<ProductModel>());
    }

    public Task<ProductModel?> GetProductModelBySlugAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return Task.FromResult<ProductModel?>(null);
        }

        return ExecuteAsync(() =>
        {
            var model = _productModelRepository.FindBySlug(slug);
            return model == null ? null : MapProductModel(model);
        }, $"load product model {slug}", (ProductModel?)null);
    }

    public Task<IReadOnlyList<Product>> GetProductsByModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
        }

        return ExecuteAsync(() =>
        {
            var products = _productRepository.GetByModelId(modelId) ?? Enumerable.Empty<PhoneStoreRepository.Models.Product>();
            return products
                .Select(MapProduct)
                .OrderByDescending(p => p.CreatedAt)
                .ToList()
                .AsReadOnly();
        }, $"load products for model {modelId}", Array.Empty<Product>());
    }

    public Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<ProductAttribute>>(Array.Empty<ProductAttribute>());
        }

        return ExecuteAsync(() =>
        {
            var attributeIds = _productModelAttributeRepository.GetAttributeIdsByModel(modelId)?.Distinct().ToList();
            if (attributeIds == null || attributeIds.Count == 0)
            {
                return Array.Empty<ProductAttribute>() as IReadOnlyList<ProductAttribute>;
            }

            var attributes = new List<ProductAttribute>(attributeIds.Count);
            foreach (var attributeId in attributeIds)
            {
                try
                {
                    var attribute = _productAttributeRepository.GetById(attributeId);
                    attributes.Add(MapProductAttribute(attribute));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load attribute {AttributeId} for model {ModelId}", attributeId, modelId);
                }
            }

            return attributes
                .OrderBy(a => a.Id)
                .ToList()
                .AsReadOnly();
        }, $"load attributes for model {modelId}", Array.Empty<ProductAttribute>());
    }

    public Task<IReadOnlyList<ProductAttributeValue>> GetAttributeValuesForProductsAsync(IEnumerable<int> productIds)
    {
        if (productIds == null)
        {
            return Task.FromResult<IReadOnlyList<ProductAttributeValue>>(Array.Empty<ProductAttributeValue>());
        }

        var distinctIds = productIds.Where(id => id > 0).Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<ProductAttributeValue>>(Array.Empty<ProductAttributeValue>());
        }

        return ExecuteAsync(() =>
        {
            var values = new List<ProductAttributeValue>();
            foreach (var productId in distinctIds)
            {
                var attributeValues = _productAttributeValueRepository.GetByProductId(productId)
                                     ?? Enumerable.Empty<PhoneStoreRepository.Models.ProductAttributeValue>();
                values.AddRange(attributeValues.Select(MapProductAttributeValue));
            }

            return values.AsReadOnly();
        }, "load attribute values for products", Array.Empty<ProductAttributeValue>());
    }

    public Task<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>> GetAttributeOptionsForAttributesAsync(IEnumerable<int> attributeIds)
    {
        if (attributeIds == null)
        {
            return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>>(new Dictionary<int, IReadOnlyList<ProductAttributeOption>>());
        }

        var ids = attributeIds.Where(id => id > 0).Distinct().ToList();
        if (ids.Count == 0)
        {
            return Task.FromResult<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>>(new Dictionary<int, IReadOnlyList<ProductAttributeOption>>());
        }

        return ExecuteAsync(() =>
        {
            var lookup = _productAttributeOptionRepository.GetByAttributeIds(ids)
                ?? new Dictionary<int, IReadOnlyList<PhoneStoreRepository.Models.ProductAttributeOption>>();

            var result = new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            foreach (var id in ids)
            {
                if (lookup.TryGetValue(id, out var options) && options != null)
                {
                    var mapped = options
                        .Select(MapProductAttributeOption)
                        .OrderBy(o => o.SortOrder)
                        .ThenBy(o => o.DisplayValue, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                        .AsReadOnly();

                    result[id] = mapped;
                }
                else
                {
                    result[id] = Array.Empty<ProductAttributeOption>();
                }
            }

            return (IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>)result;
        }, "load attribute options", new Dictionary<int, IReadOnlyList<ProductAttributeOption>>());
    }

    private Task<T> ExecuteAsync<T>(Func<T> action, string operationDescription, T? fallback)
    {
        return Task.Run(() =>
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to {Operation}", operationDescription);
                return fallback!;
            }
        });
    }

    private static ProductModel MapProductModel(PhoneStoreRepository.Models.ProductModel model) =>
        new(
            Id: model.Id,
            Name: model.Name,
            Slug: model.Slug,
            Description: model.Description,
            Image: model.DefaultImageUrl,
            CreatedAt: model.CreatedAt,
            UpdatedAt: model.UpdatedAt);

    private static Product MapProduct(PhoneStoreRepository.Models.Product product) =>
        new(
            Id: product.Id,
            Sku: product.Sku,
            Name: product.Name,
            CategoryId: product.CategoryId,
            ModelId: product.ModelId,
            BrandId: product.BrandId,
            Price: product.Price,
            Cost: product.Cost,
            IsSerialTracked: product.IsSerialTracked,
            WarrantyMonths: product.WarrantyMonths,
            Status: product.Status.ToString().ToLowerInvariant(),
            CreatedAt: product.CreatedAt);

    private static ProductAttribute MapProductAttribute(PhoneStoreRepository.Models.ProductAttribute attribute) =>
        new(
            Id: attribute.Id,
            Name: attribute.Name,
            DataType: attribute.DataType.ToString().ToLowerInvariant(),
            Note: attribute.Note);

    private static ProductAttributeValue MapProductAttributeValue(PhoneStoreRepository.Models.ProductAttributeValue attributeValue) =>
        new(
            Id: attributeValue.Id,
            ProductId: attributeValue.ProductId,
            AttributeId: attributeValue.AttributeId,
            OptionId: attributeValue.OptionId,
            ValueText: attributeValue.ValueText,
            ValueNumber: attributeValue.ValueNumber,
            ValueDate: attributeValue.ValueDate,
            ValueBool: attributeValue.ValueBool);

    private static ProductAttributeOption MapProductAttributeOption(PhoneStoreRepository.Models.ProductAttributeOption option) =>
        new(
            Id: option.Id,
            AttributeId: option.AttributeId,
            DisplayValue: option.DisplayValue,
            NormalizedValue: option.NormalizedValue,
            SortOrder: option.SortOrder,
            IsActive: option.IsActive,
            CreatedAt: option.CreatedAt,
            UpdatedAt: option.UpdatedAt);
}
