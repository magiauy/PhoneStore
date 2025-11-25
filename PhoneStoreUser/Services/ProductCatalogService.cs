using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PhoneStoreUser.Components.Models;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class ProductCatalogService : IProductCatalogService
{
    private readonly ILogger<ProductCatalogService> _logger;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProductCatalogService(
        ILogger<ProductCatalogService> logger,
        IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public Task<IReadOnlyList<ProductModel>> GetProductModelsAsync()
    {
        return ExecuteAsync<IReadOnlyList<ProductModel>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var models = context.ProductModels.AsNoTracking()
                .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
                .ThenBy(m => m.Name)
                .ToList();

            return models.Select(MapProductModel).ToList().AsReadOnly();
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
            using var context = _dbContextFactory.CreateDbContext();
            var model = context.ProductModels.AsNoTracking().FirstOrDefault(m => m.Slug == slug);
            return model == null ? null : MapProductModel(model);
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

            // Find category by name (treating input slug as name)
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

            var models = context.ProductModels.AsNoTracking()
                .Where(m => modelIds.Contains(m.Id))
                .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
                .ThenBy(m => m.Name)
                .ToList();

            return models.Select(MapProductModel).ToList().AsReadOnly();
        }, $"load product models for category {categorySlug}", Array.Empty<ProductModel>());
    }

    public Task<IReadOnlyList<Product>> GetProductsByModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
        }

        return ExecuteAsync<IReadOnlyList<Product>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var query = from p in context.Products.AsNoTracking()
                        where p.ModelId == modelId
                        join b in context.Brands.AsNoTracking()
                            on p.BrandId equals (int?)b.Id into pb
                        from b in pb.DefaultIfEmpty()
                        select new { P = p, BrandName = b != null ? b.Name : string.Empty };

            var list = query
                .OrderByDescending(x => x.P.CreatedAt)
                .ToList()
                .Select(x => new Product(
                    x.P.Id,
                    x.P.Sku,
                    x.P.Name,
                    x.P.CategoryId,
                    x.P.ModelId,
                    x.P.BrandId,
                    x.BrandName,
                    x.P.Price,
                    x.P.Cost,
                    x.P.IsSerialTracked,
                    x.P.WarrantyMonths,
                    (x.P.Status ?? "active").ToLower(),
                    x.P.CreatedAt,
                    null
                ))
                .ToList()
                .AsReadOnly();

            return list;
        }, $"load products for model {modelId}", Array.Empty<Product>());
    }

    public Task<IReadOnlyList<ProductAttribute>> GetAttributesForModelAsync(int modelId)
    {
        if (modelId <= 0)
        {
            return Task.FromResult<IReadOnlyList<ProductAttribute>>(Array.Empty<ProductAttribute>());
        }

        return ExecuteAsync<IReadOnlyList<ProductAttribute>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var attributeIds = context.ProductModelAttributes.AsNoTracking()
                .Where(pma => pma.ModelId == modelId)
                .Select(pma => pma.AttributeId)
                .Distinct()
                .ToList();

            if (attributeIds.Count == 0)
            {
                return Array.Empty<ProductAttribute>() as IReadOnlyList<ProductAttribute>;
            }

            var attributes = context.ProductAttributes.AsNoTracking()
                .Where(a => attributeIds.Contains(a.Id))
                .OrderBy(a => a.Id)
                .ToList();

            return attributes.Select(MapProductAttribute).ToList().AsReadOnly();
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
            using var context = _dbContextFactory.CreateDbContext();
            var attributeValues = context.ProductAttributeValues.AsNoTracking()
                .Where(av => distinctIds.Contains(av.ProductId))
                .ToList();

            return attributeValues.Select(MapProductAttributeValue).ToList().AsReadOnly();
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
            using var context = _dbContextFactory.CreateDbContext();
            var options = context.ProductAttributeOptions.AsNoTracking()
                .Where(o => ids.Contains(o.AttributeId))
                .ToList();

            var result = new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();
            foreach (var id in ids)
            {
                var attributeOptions = options
                    .Where(o => o.AttributeId == id)
                    .Select(MapProductAttributeOption)
                    .OrderBy(o => o.SortOrder)
                    .ThenBy(o => o.DisplayValue, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();

                result[id] = attributeOptions;
            }

            return (IReadOnlyDictionary<int, IReadOnlyList<ProductAttributeOption>>)result;
        }, "load attribute options", new Dictionary<int, IReadOnlyList<ProductAttributeOption>>());
    }

    public Task<IReadOnlyList<Brand>> GetBrandsAsync()
    {
        return ExecuteAsync<IReadOnlyList<Brand>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            return context.Brands.AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new Brand(b.Id, b.Name))
                .ToList()
                .AsReadOnly();
        }, "load brands", Array.Empty<Brand>());
    }

    public Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync()
    {
        return ExecuteAsync<IReadOnlyList<ProductCategory>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            return context.Categories.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new ProductCategory(c.Id, c.Name, c.ParentId, c.Note))
                .ToList()
                .AsReadOnly();
        }, "load categories", Array.Empty<ProductCategory>());
    }

    public Task<IReadOnlyList<ProductModel>> GetFilteredProductModelsAsync(IEnumerable<int>? brandIds = null, IEnumerable<int>? categoryIds = null)
    {
        return ExecuteAsync<IReadOnlyList<ProductModel>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();

            var productQuery = context.Products.AsNoTracking()
                .Where(p => (brandIds == null || !brandIds.Any() || brandIds.Contains(p.BrandId ?? 0)) &&
                            (categoryIds == null || !categoryIds.Any() || categoryIds.Contains(p.CategoryId)));

            var modelIds = productQuery
                .Select(p => p.ModelId)
                .Distinct()
                .ToList();

            if (modelIds.Count == 0)
            {
                return Array.Empty<ProductModel>();
            }

            var models = context.ProductModels.AsNoTracking()
                .Where(m => modelIds.Contains(m.Id))
                .OrderByDescending(m => m.UpdatedAt ?? m.CreatedAt)
                .ThenBy(m => m.Name)
                .ToList();

            return models.Select(MapProductModel).ToList().AsReadOnly();
        }, "load filtered product models", Array.Empty<ProductModel>());
    }

    public Task<IReadOnlyList<ProductCardViewModel>> GetFilteredProductsAsync(string? searchTerm = null, IEnumerable<int>? brandIds = null, IEnumerable<int>? categoryIds = null)
    {
        return ExecuteAsync<IReadOnlyList<ProductCardViewModel>>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();

            var currentQuery = from p in context.Products.AsNoTracking()
                               join pm in context.ProductModels.AsNoTracking() on p.ModelId equals pm.Id
                               join b in context.Brands.AsNoTracking()
                                   on p.BrandId equals (int?)b.Id into pb
                               from b in pb.DefaultIfEmpty()
                               select new { P = p, Model = pm, BrandName = b != null ? b.Name : string.Empty };

            if (brandIds != null && brandIds.Any())
            {
                currentQuery = currentQuery.Where(x => x.P.BrandId.HasValue && brandIds.Contains(x.P.BrandId.Value));
            }

            if (categoryIds != null && categoryIds.Any())
            {
                currentQuery = currentQuery.Where(x => categoryIds.Contains(x.P.CategoryId));
            }

            var normalizedSearchTerm = searchTerm?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(normalizedSearchTerm))
            {
                var scoredQuery = currentQuery.Select(x => new
                {
                    x.P,
                    x.Model,
                    x.BrandName,
                    RelevanceScore =
                        (x.P.Sku != null && x.P.Sku.ToLower() == normalizedSearchTerm ? 50 : 0) +

                        (x.P.Name != null && x.P.Name.ToLower().StartsWith(normalizedSearchTerm) ? 30 : 0) +

                        (x.P.Name != null && x.P.Name.ToLower().Contains(normalizedSearchTerm) ? 10 : 0) +

                        (x.Model.Name != null && x.Model.Name.ToLower().Contains(normalizedSearchTerm) ? 5 : 0) +

                        (x.P.Sku != null && x.P.Sku.ToLower().Contains(normalizedSearchTerm) && x.P.Sku.ToLower() != normalizedSearchTerm ? 5 : 0) +

                        (x.BrandName != null && x.BrandName.ToLower().Contains(normalizedSearchTerm) ? 3 : 0)
                });

                scoredQuery = scoredQuery.Where(x => x.RelevanceScore > 0);

                return scoredQuery
                    .OrderByDescending(x => x.RelevanceScore)
                    .ThenByDescending(x => x.P.CreatedAt)
                    .Select(x => new ProductCardViewModel(
                        x.P.Id,
                        x.P.Sku,
                        x.P.Name,
                        x.P.CategoryId,
                        x.P.ModelId,
                        x.P.BrandId,
                        x.BrandName,
                        x.P.Price,
                        x.P.Cost,
                        x.P.IsSerialTracked,
                        x.P.WarrantyMonths,
                        (x.P.Status ?? "active").ToLower(),
                        x.P.CreatedAt,
                        null,
                        x.Model.Slug
                    ))
                    .ToList()
                    .AsReadOnly();
            }
            else
            {
                return currentQuery
                    .OrderByDescending(x => x.P.CreatedAt)
                    .Select(x => new ProductCardViewModel(
                        x.P.Id,
                        x.P.Sku,
                        x.P.Name,
                        x.P.CategoryId,
                        x.P.ModelId,
                        x.P.BrandId,
                        x.BrandName,
                        x.P.Price,
                        x.P.Cost,
                        x.P.IsSerialTracked,
                        x.P.WarrantyMonths,
                        (x.P.Status ?? "active").ToLower(),
                        x.P.CreatedAt,
                        null,
                        x.Model.Slug
                    ))
                    .ToList()
                    .AsReadOnly();
            }
        }, "load filtered products", Array.Empty<ProductCardViewModel>());
    }

    public Task<Product?> GetProductBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Task.FromResult<Product?>(null);
        }

        return ExecuteAsync<Product?>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var query = from p in context.Products.AsNoTracking()
                        where p.Sku.ToLower() == sku.ToLower()
                        join b in context.Brands.AsNoTracking()
                            on p.BrandId equals (int?)b.Id into pb
                        from b in pb.DefaultIfEmpty()
                        select new { P = p, BrandName = b != null ? b.Name : string.Empty };

            var result = query.FirstOrDefault();
            if (result == null) return null;

            return new Product(
                result.P.Id,
                result.P.Sku,
                result.P.Name,
                result.P.CategoryId,
                result.P.ModelId,
                result.P.BrandId,
                result.BrandName,
                result.P.Price,
                result.P.Cost,
                result.P.IsSerialTracked,
                result.P.WarrantyMonths,
                result.P.Status,
                result.P.CreatedAt,
                null
            );
        }, $"load product {sku}", (Product?)null);
    }

    public Task<ProductModel?> GetProductModelByIdAsync(int id)
    {
        if (id <= 0)
        {
            return Task.FromResult<ProductModel?>(null);
        }

        return ExecuteAsync<ProductModel?>(() =>
        {
            using var context = _dbContextFactory.CreateDbContext();
            var model = context.ProductModels.AsNoTracking().FirstOrDefault(m => m.Id == id);
            return model == null ? null : MapProductModel(model);
        }, $"load product model {id}", (ProductModel?)null);
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

    private static ProductModel MapProductModel(PhoneStoreUser.Data.ProductModelEntity model) =>
        new(
            Id: model.Id,
            Name: model.Name ?? string.Empty,
            Slug: model.Slug ?? string.Empty,
            Description: model.Description,
            Image: model.DefaultImageUrl,
            CreatedAt: model.CreatedAt,
            UpdatedAt: model.UpdatedAt);

    private static Product MapProduct(PhoneStoreUser.Data.ProductEntity product) =>
        new(
            Id: product.Id,
            Sku: product.Sku,
            Name: product.Name,
            CategoryId: product.CategoryId,
            ModelId: product.ModelId,
            BrandId: product.BrandId,
            BrandName: string.Empty,
            Price: product.Price,
            Cost: product.Cost,
            IsSerialTracked: product.IsSerialTracked,
            WarrantyMonths: product.WarrantyMonths,
            Status: product.Status.ToString().ToLowerInvariant(),
            CreatedAt: product.CreatedAt);

    private static ProductAttribute MapProductAttribute(PhoneStoreUser.Data.ProductAttributeEntity attribute) =>
        new(
            Id: attribute.Id,
            Name: attribute.Name,
            DataType: attribute.DataType.ToString().ToLowerInvariant(),
            Note: attribute.Note);

    private static ProductAttributeValue MapProductAttributeValue(PhoneStoreUser.Data.ProductAttributeValueEntity attributeValue) =>
        new(
            Id: attributeValue.Id,
            ProductId: attributeValue.ProductId,
            AttributeId: attributeValue.AttributeId,
            OptionId: attributeValue.OptionId,
            ValueText: attributeValue.ValueText,
            ValueNumber: attributeValue.ValueNumber,
            ValueDate: attributeValue.ValueDate,
            ValueBool: attributeValue.ValueBool);

    private static ProductAttributeOption MapProductAttributeOption(PhoneStoreUser.Data.ProductAttributeOptionEntity option) =>
        new(
            Id: option.Id,
            AttributeId: option.AttributeId,
            DisplayValue: option.DisplayValue,
            NormalizedValue: option.NormalizedValue.ToString(),
            SortOrder: option.SortOrder,
            IsActive: option.IsActive != 0,
            CreatedAt: option.CreatedAt,
            UpdatedAt: option.UpdatedAt);
}
