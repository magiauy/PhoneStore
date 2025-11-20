using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

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
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProductCatalogService(
        IProductModelRepository productModelRepository,
        IProductRepository productRepository,
        IProductModelAttributeRepository productModelAttributeRepository,
        IProductAttributeRepository productAttributeRepository,
        IProductAttributeValueRepository productAttributeValueRepository,
        IProductAttributeOptionRepository productAttributeOptionRepository,
        ILogger<ProductCatalogService> logger,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _productModelRepository = productModelRepository;
        _productRepository = productRepository;
        _productModelAttributeRepository = productModelAttributeRepository;
        _productAttributeRepository = productAttributeRepository;
        _productAttributeValueRepository = productAttributeValueRepository;
        _productAttributeOptionRepository = productAttributeOptionRepository;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public Task<IReadOnlyList<ProductModel>> GetProductModelsAsync()
    {
        return ExecuteAsync<IReadOnlyList<ProductModel>>(() =>
        {
            var models = _productModelRepository.GetAll() ?? Enumerable.Empty<ProductModel>();
            return models
                .OrderByDescending(m => m.UpdatedAt)
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

        return ExecuteAsync<ProductModel?>(() =>
        {
            var model = _productModelRepository.FindBySlug(slug);
            return model;
        }, $"load product model {slug}", (ProductModel?)null);
    }

    public Task<IReadOnlyList<ProductModel>> GetProductModelsByCategorySlugAsync(string categorySlug)
    {
        if (string.IsNullOrWhiteSpace(categorySlug))
        {
            return Task.FromResult<IReadOnlyList<ProductModel>>(Array.Empty<ProductModel>());
        }

        return ExecuteAsync<IReadOnlyList<ProductModel>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();

            var category = context.Categories.AsNoTracking()
                .FirstOrDefault(c => c.Name == categorySlug);

            if (category == null)
            {
                return Array.Empty<ProductModel>();
            }

            var modelIds = context.Products.AsNoTracking()
                .Where(p => p.CategoryId == category.Id)
                .Select(p => p.ModelId)
                .Distinct()
                .ToList();

            if (modelIds.Count == 0)
            {
                return Array.Empty<ProductModel>();
            }

            var allModels = _productModelRepository.GetAll() ?? Enumerable.Empty<ProductModel>();

            return allModels
                .Where(m => modelIds.Contains(m.Id))
                .OrderByDescending(m => m.UpdatedAt)
                .ThenBy(m => m.Name)
                .ToList()
                .AsReadOnly();
        }, $"load product models for category {categorySlug}", Array.Empty<ProductModel>());
    }

    public Task<IReadOnlyList<ProductVariantViewModel>> GetProductsByModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<ProductVariantViewModel>>(Array.Empty<ProductVariantViewModel>());
        }

        return ExecuteAsync<IReadOnlyList<ProductVariantViewModel>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var products = _productRepository.GetByModelId(modelId)?.ToList() ?? new List<Product>();

            var brandIds = products
                .Select(p => p.BrandId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            var brandLookup = brandIds.Count == 0
                ? new Dictionary<int, string>()
                : context.Brands.AsNoTracking()
                    .Where(b => brandIds.Contains(b.Id))
                    .ToDictionary(b => b.Id, b => b.Name);

            var list = products
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new ProductVariantViewModel(
                    p,
                    brandLookup.TryGetValue(p.BrandId ?? 0, out var brand) ? brand : string.Empty))
                .ToList()
                .AsReadOnly();

            return list;
        }, $"load products for model {modelId}", Array.Empty<ProductVariantViewModel>());
    }

    public Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<ProductAttribute>>(Array.Empty<ProductAttribute>());
        }

        return ExecuteAsync<IReadOnlyList<ProductAttribute>>(() =>
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
                    attributes.Add(attribute);
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

        return ExecuteAsync<IReadOnlyList<ProductAttributeValue>>(() =>
        {
            var values = new List<ProductAttributeValue>();
            foreach (var productId in distinctIds)
            {
                var attributeValues = _productAttributeValueRepository.GetByProductId(productId)
                                     ?? Enumerable.Empty<ProductAttributeValue>();
                values.AddRange(attributeValues);
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

        return ExecuteAsync<IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>>(() =>
        {
            var lookup = _productAttributeOptionRepository.GetByAttributeIds(ids)
                ?? new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();

            var result = new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            foreach (var id in ids)
            {
                if (lookup.TryGetValue(id, out var options) && options != null)
                {
                    var mapped = options
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
}
