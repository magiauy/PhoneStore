using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly IProductCategoryRepository _productCategoryRepository;
        private readonly IProductAttributeRepository _productAttributeRepository;
        private readonly IProductAttributeValueRepository _productAttributeValueRepository;
        private readonly IProductSerialRepository _productSerialRepository;
        private readonly IProductModelRepository _productModelRepository;
        private readonly IProductAttributeOptionService _productAttributeOptionService;

        private readonly Dictionary<int, string> _modelNameCache = new();
        private readonly Dictionary<int, IReadOnlyList<ProductAttributeOption>> _attributeOptionCache = new();
        private bool _seedAttempted;

        public ProductService(
            IProductRepository productRepository,
            IBrandRepository brandRepository,
            IProductCategoryRepository productCategoryRepository,
            IProductAttributeRepository productAttributeRepository,
            IProductAttributeValueRepository productAttributeValueRepository,
            IProductSerialRepository productSerialRepository,
            IProductModelRepository productModelRepository,
            IProductAttributeOptionService productAttributeOptionService)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
            _productCategoryRepository = productCategoryRepository ?? throw new ArgumentNullException(nameof(productCategoryRepository));
            _productAttributeRepository = productAttributeRepository ?? throw new ArgumentNullException(nameof(productAttributeRepository));
            _productAttributeValueRepository = productAttributeValueRepository ?? throw new ArgumentNullException(nameof(productAttributeValueRepository));
            _productSerialRepository = productSerialRepository ?? throw new ArgumentNullException(nameof(productSerialRepository));
            _productModelRepository = productModelRepository ?? throw new ArgumentNullException(nameof(productModelRepository));
            _productAttributeOptionService = productAttributeOptionService ?? throw new ArgumentNullException(nameof(productAttributeOptionService));

            EnsureSeedData();
        }

        public ProductSearchResult SearchProducts(ProductFilterCriteria criteria)
        {
            if (criteria == null)
            {
                Logger.Warning("Product search criteria is null. Returning empty result.");
                return new ProductSearchResult(Array.Empty<ProductListItemViewModel>(), new InfoTable(0, 0));
            }

            criteria.Normalize();
            return SearchProducts(criteria.Sku, criteria.Name, criteria.CategoryId, criteria.BrandId, criteria.Status, criteria.Page, criteria.PageSize);
        }

        public IReadOnlyList<ProductModel> GetAllModels()
        {
            try
            {
                var models = _productModelRepository.GetAll()?.OrderBy(m => m.Name ?? string.Empty).ToList() ?? new List<ProductModel>();
                foreach (var model in models)
                {
                    _modelNameCache[model.Id] = model.Name ?? string.Empty;
                }

                return models;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load product models", ex);
                return Array.Empty<ProductModel>();
            }
        }

        public IReadOnlyList<ProductModelListItemViewModel> GetProductModelSummaries()
        {
            try
            {
                var models = _productModelRepository.GetAll()?.ToList() ?? new List<ProductModel>();
                var products = _productRepository.GetAll()?.ToList() ?? new List<Product>();
                var variantLookup = products
                    .GroupBy(p => p.ModelId)
                    .ToDictionary(group => group.Key, group => group.Count());

                var summaries = new List<ProductModelListItemViewModel>(models.Count);
                foreach (var model in models)
                {
                    _modelNameCache[model.Id] = model.Name ?? string.Empty;
                    variantLookup.TryGetValue(model.Id, out var variantCount);
                    summaries.Add(new ProductModelListItemViewModel
                    {
                        Id = model.Id,
                        Name = model.Name ?? string.Empty,
                        Description = model.Description,
                        DefaultImageUrl = model.DefaultImageUrl,
                        VariantCount = variantCount,
                        CreatedAt = model.CreatedAt,
                        UpdatedAt = model.UpdatedAt
                    });
                }

                return summaries
                    .OrderByDescending(m => m.UpdatedAt)
                    .ThenBy(m => m.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load product model summaries", ex);
                return Array.Empty<ProductModelListItemViewModel>();
            }
        }

        public ProductModelDetailViewModel? GetProductModelDetail(int modelId)
        {
            try
            {
                var model = _productModelRepository.GetById(modelId);
                if (model == null)
                {
                    return null;
                }

                _modelNameCache[model.Id] = model.Name ?? string.Empty;

                var variants = _productRepository.GetByModelId(modelId)?.ToList() ?? new List<Product>();
                var categoryLookup = BuildCategoryLookup();
                var brandLookup = BuildBrandLookup();
                var serialLookup = variants.Count > 0
                    ? _productSerialRepository.GetCountsByProductIds(variants.Select(v => v.Id).ToList())
                    : new Dictionary<int, int>();

                var variantViewModels = variants
                    .Select(product =>
                    {
                        var categoryName = categoryLookup.TryGetValue(product.CategoryId, out var catName) ? catName : string.Empty;
                        string? brandName = null;
                        if (product.BrandId.HasValue && brandLookup.TryGetValue(product.BrandId.Value, out var bName))
                        {
                            brandName = bName;
                        }

                        var serialCount = serialLookup.TryGetValue(product.Id, out var count)
                            ? count
                            : 0;

                        return new ProductListItemViewModel(product, categoryName, brandName, serialCount, model.Name);
                    })
                    .OrderByDescending(p => p.CreatedAt)
                    .ToList();

                var modelViewModel = new ProductModelListItemViewModel
                {
                    Id = model.Id,
                    Name = model.Name ?? string.Empty,
                    Description = model.Description,
                    DefaultImageUrl = model.DefaultImageUrl,
                    VariantCount = variantViewModels.Count,
                    CreatedAt = model.CreatedAt,
                    UpdatedAt = model.UpdatedAt
                };

                return new ProductModelDetailViewModel(modelViewModel, variantViewModels);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to load product model detail for {modelId}", ex);
                return null;
            }
        }

        public bool SaveProductModel(ProductModel model)
        {
            if (model == null)
            {
                Logger.Warning("Attempted to save null product model");
                return false;
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                Logger.Warning("Product model name is required");
                return false;
            }

            try
            {
                var now = DateTime.UtcNow;
                var baseSlug = string.IsNullOrWhiteSpace(model.Slug) ? model.Name : model.Slug;
                var normalizedSlug = GenerateSlug(baseSlug);
                model.Slug = EnsureUniqueModelSlug(normalizedSlug, model.Id);
                model.UpdatedAt = now;

                if (model.Id <= 0)
                {
                    model.CreatedAt = now;
                    _productModelRepository.Insert(model);
                }
                else
                {
                    _productModelRepository.Update(model);
                }

                _modelNameCache[model.Id] = model.Name ?? string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to save product model", ex);
                return false;
            }
        }

        public ProductModel? DuplicateProductModel(int sourceModelId, string name, string? description, string? defaultImageUrl)
        {
            try
            {
                var source = _productModelRepository.GetById(sourceModelId);
                if (source == null)
                {
                    Logger.Warning($"Source product model {sourceModelId} not found for duplication");
                    return null;
                }

                var now = DateTime.UtcNow;
                var duplicate = new ProductModel
                {
                    Name = string.IsNullOrWhiteSpace(name) ? $"{source.Name} Copy" : name,
                    Description = string.IsNullOrWhiteSpace(description) ? source.Description : description,
                    DefaultImageUrl = string.IsNullOrWhiteSpace(defaultImageUrl) ? source.DefaultImageUrl : defaultImageUrl,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                var baseSlug = GenerateSlug(duplicate.Name);
                duplicate.Slug = EnsureUniqueModelSlug(baseSlug, 0);

                _productModelRepository.Insert(duplicate);
                _modelNameCache[duplicate.Id] = duplicate.Name ?? string.Empty;

                return duplicate;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to duplicate product model {sourceModelId}", ex);
                return null;
            }
        }

        public IReadOnlyList<ProductAttributeDefinition> GetAttributeDefinitions()
        {
            try
            {
                var attributes = _productAttributeRepository.GetAll()?.OrderBy(a => a.Name).ToList() ?? new List<ProductAttribute>();
                var optionsLookup = _productAttributeOptionService.GetOptionsForAttributes(attributes.Select(a => a.Id))
                    ?? new Dictionary<int, IReadOnlyList<ProductAttributeOption>>();

                foreach (var pair in optionsLookup)
                {
                    _attributeOptionCache[pair.Key] = pair.Value;
                }

                var definitions = new List<ProductAttributeDefinition>(attributes.Count);
                foreach (var attribute in attributes)
                {
                    optionsLookup.TryGetValue(attribute.Id, out var options);
                    options ??= Array.Empty<ProductAttributeOption>();
                    definitions.Add(new ProductAttributeDefinition(attribute, options));
                }

                return definitions;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load product attribute definitions", ex);
                return Array.Empty<ProductAttributeDefinition>();
            }
        }

        public ProductSearchResult SearchProducts(string? sku, string? name, int? categoryId, int? brandId, ProductStatus? status, int page = 1, int pageSize = 20)
        {
            try
            {
                if (page <= 0)
                {
                    Logger.Warning($"Invalid page {page}. Resetting to 1.");
                    page = 1;
                }

                if (pageSize <= 0)
                {
                    Logger.Warning($"Invalid page size {pageSize}. Resetting to 20.");
                    pageSize = 20;
                }

                var products = _productRepository.GetAll();

                var filtered = products.Where(p =>
                    (string.IsNullOrWhiteSpace(sku) || p.Sku.Contains(sku, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrWhiteSpace(name) || p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)) &&
                    (!categoryId.HasValue || p.CategoryId == categoryId.Value) &&
                    (!brandId.HasValue || p.BrandId == brandId.Value) &&
                    (!status.HasValue || p.Status == status.Value));

                var totalRecords = filtered.Count();
                var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
                var pagedProducts = filtered
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var categoryLookup = BuildCategoryLookup();
                var brandLookup = BuildBrandLookup();
                var modelLookup = BuildModelLookup();

                var productIds = pagedProducts.Select(p => p.Id).ToList();
                var serialCounts = productIds.Count > 0
                    ? _productSerialRepository.GetCountsByProductIds(productIds)
                    : new Dictionary<int, int>();

                var viewModels = pagedProducts
                    .Select(p =>
                    {
                        var categoryName = categoryLookup.TryGetValue(p.CategoryId, out var catName) ? catName : string.Empty;
                        string? brandName = null;
                        if (p.BrandId.HasValue && brandLookup.TryGetValue(p.BrandId.Value, out var brand))
                        {
                            brandName = brand;
                        }

                        var serialCount = serialCounts.TryGetValue(p.Id, out var count) ? count : 0;
                        var modelName = modelLookup.TryGetValue(p.ModelId, out var mName) ? mName : string.Empty;

                        return new ProductListItemViewModel(p, categoryName, brandName, serialCount, modelName);
                    })
                    .ToList();

                return new ProductSearchResult(viewModels, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to search products", ex);
                return new ProductSearchResult(Array.Empty<ProductListItemViewModel>(), new InfoTable(0, 0));
            }
        }

        public ProductDetailViewModel? GetProductDetail(int productId)
        {
            try
            {
                var product = _productRepository.GetById(productId);
                if (product == null)
                {
                    Logger.Warning($"Product {productId} not found.");
                    return null;
                }

                var categoryName = TryGetCategoryName(product.CategoryId);
                var brandName = product.BrandId.HasValue ? TryGetBrandName(product.BrandId.Value) : null;
                var modelName = TryGetModelName(product.ModelId);

                var attributeValues = _productAttributeValueRepository.GetByProductId(productId).ToList();
                var attributeViewModels = attributeValues
                    .Select(value => BuildAttributeViewModel(value))
                    .Where(vm => vm != null)
                    .Cast<ProductAttributeValueViewModel>()
                    .ToList();

                var serials = _productSerialRepository.GetByProductId(productId).ToList();
                SynchronizeSerialTracking(product, serials);

                var serialViewModels = serials.Select(serial => new ProductSerialViewModel
                {
                    Id = serial.Id,
                    SerialNumber = serial.SerialNumber,
                    Imei1 = serial.Imei1,
                    Imei2 = serial.Imei2,
                    BatchId = serial.BatchId,
                    Status = serial.Status,
                    PurchaseOrderLineId = serial.PurchaseOrderLineId,
                    Note = serial.Note
                }).ToList();

                var productViewModel = new ProductListItemViewModel(product, categoryName, brandName, serialViewModels.Count, modelName);

                return new ProductDetailViewModel(productViewModel, attributeViewModels, serialViewModels);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get product detail for {productId}", ex);
                return null;
            }
        }

        public bool CreateProduct(Product product, IEnumerable<ProductAttributeValueInput>? attributeValues = null)
        {
            if (product == null)
            {
                Logger.Warning("Cannot create a null product.");
                return false;
            }

            try
            {
                if (!ValidateProduct(product, isUpdate: false))
                {
                    return false;
                }

                var attributeValueList = attributeValues?.ToList();
                if (attributeValueList != null && !ValidateAttributeInputs(attributeValueList))
                {
                    return false;
                }

                product.CreatedAt = DateTime.UtcNow;
                _productRepository.Insert(product);

                if (attributeValueList != null)
                {
                    try
                    {
                        SaveAttributeValuesInternal(product.Id, attributeValueList, overwriteMissing: true);
                    }
                    catch
                    {
                        _productRepository.Delete(product.Id);
                        throw;
                    }
                }

                Logger.Info($"Created product {product.Id} ({product.Sku}).");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to create product", ex);
                return false;
            }
        }

        public bool UpdateProduct(Product product, IEnumerable<ProductAttributeValueInput>? attributeValues = null)
        {
            if (product == null)
            {
                Logger.Warning("Cannot update a null product.");
                return false;
            }

            try
            {
                if (!ValidateProduct(product, isUpdate: true))
                {
                    return false;
                }

                var attributeValueList = attributeValues?.ToList();
                if (attributeValueList != null && !ValidateAttributeInputs(attributeValueList))
                {
                    return false;
                }

                _productRepository.Update(product);

                if (attributeValueList != null)
                {
                    SaveAttributeValuesInternal(product.Id, attributeValueList, overwriteMissing: true);
                }

                SynchronizeSerialTracking(product, null);

                Logger.Info($"Updated product {product.Id} ({product.Sku}).");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update product {product?.Id}", ex);
                return false;
            }
        }

        public bool DeleteProduct(int productId)
        {
            try
            {
                var attributes = _productAttributeValueRepository.GetByProductId(productId).ToList();
                foreach (var attribute in attributes)
                {
                    _productAttributeValueRepository.Delete(attribute.Id);
                }

                var serials = _productSerialRepository.GetByProductId(productId).ToList();
                foreach (var serial in serials)
                {
                    _productSerialRepository.Delete(serial.Id);
                }

                _productRepository.Delete(productId);

                Logger.Info($"Deleted product {productId} and related data.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete product {productId}", ex);
                return false;
            }
        }

        public bool SaveAttributeValues(int productId, IEnumerable<ProductAttributeValueInput> attributeValues)
        {
            try
            {
                var attributeValueList = attributeValues?.ToList();
                if (attributeValueList != null && !ValidateAttributeInputs(attributeValueList))
                {
                    return false;
                }

                SaveAttributeValuesInternal(productId, attributeValueList ?? Enumerable.Empty<ProductAttributeValueInput>(), overwriteMissing: false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to save attribute values for product {productId}", ex);
                return false;
            }
        }

        #region Helpers

        private Dictionary<int, string> BuildCategoryLookup()
        {
            try
            {
                return _productCategoryRepository.GetAll()
                    .GroupBy(c => c.Id)
                    .ToDictionary(c => c.Key, c => c.First().Name ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to build product category lookup", ex);
                return new Dictionary<int, string>();
            }
        }

        private Dictionary<int, string> BuildBrandLookup()
        {
            try
            {
                return _brandRepository.GetAll()
                    .GroupBy(b => b.Id)
                    .ToDictionary(b => b.Key, b => b.First().Name ?? string.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to build brand lookup", ex);
                return new Dictionary<int, string>();
            }
        }

        private Dictionary<int, string> BuildModelLookup()
        {
            try
            {
                var models = _productModelRepository.GetAll()?.ToList() ?? new List<ProductModel>();
                foreach (var model in models)
                {
                    _modelNameCache[model.Id] = model.Name ?? string.Empty;
                }

                return _modelNameCache.ToDictionary(pair => pair.Key, pair => pair.Value);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to build model lookup", ex);
                return new Dictionary<int, string>();
            }
        }

        private string TryGetCategoryName(int categoryId)
        {
            try
            {
                var category = _productCategoryRepository.GetById(categoryId);
                return category?.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve category name for {categoryId}", ex);
                return string.Empty;
            }
        }

        private string? TryGetBrandName(int brandId)
        {
            try
            {
                var brand = _brandRepository.GetById(brandId);
                return brand?.Name;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve brand name for {brandId}", ex);
                return null;
            }
        }

        private string TryGetModelName(int modelId)
        {
            if (modelId <= 0)
            {
                return string.Empty;
            }

            if (_modelNameCache.TryGetValue(modelId, out var cached))
            {
                return cached;
            }

            try
            {
                var model = _productModelRepository.GetById(modelId);
                var name = model?.Name ?? string.Empty;
                _modelNameCache[modelId] = name;
                return name;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to resolve model name for {modelId}", ex);
                return string.Empty;
            }
        }

        private ProductAttributeValueViewModel? BuildAttributeViewModel(ProductAttributeValue value)
        {
            try
            {
                var attribute = _productAttributeRepository.GetById(value.AttributeId);
                if (attribute == null)
                {
                    return null;
                }

                ProductAttributeOption? option = null;
                if (value.OptionId.HasValue)
                {
                    var options = GetOptionsForAttribute(attribute.Id);
                    option = options.FirstOrDefault(o => o.Id == value.OptionId.Value);
                }

                return new ProductAttributeValueViewModel
                {
                    AttributeId = value.AttributeId,
                    AttributeName = attribute.Name,
                    DataType = attribute.DataType,
                    ValueText = value.ValueText,
                    ValueNumber = value.ValueNumber,
                    ValueDate = value.ValueDate,
                    ValueBool = value.ValueBool,
                    OptionId = value.OptionId,
                    OptionDisplayValue = option?.DisplayValue
                };
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to map attribute value {value?.Id}", ex);
                return null;
            }
        }

        private bool ValidateAttributeInputs(IEnumerable<ProductAttributeValueInput>? attributeValues)
        {
            if (attributeValues == null)
            {
                return true;
            }

            foreach (var attributeValue in attributeValues)
            {
                if (attributeValue == null)
                {
                    continue;
                }

                try
                {
                    var attribute = _productAttributeRepository.GetById(attributeValue.AttributeId);
                    if (attribute == null)
                    {
                        Logger.Warning($"Attribute {attributeValue.AttributeId} not found when validating input.");
                        return false;
                    }

                    var dummy = new ProductAttributeValue
                    {
                        ProductId = 0,
                        AttributeId = attributeValue.AttributeId
                    };

                    var option = ResolveAttributeOption(attribute, attributeValue);
                    ApplyAttributeValue(attribute, attributeValue, dummy, option);
                }
                catch (FormatException ex)
                {
                    Logger.Error($"Invalid value for attribute {attributeValue?.AttributeId}", ex);
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to validate attribute {attributeValue?.AttributeId}", ex);
                    return false;
                }
            }

            return true;
        }

        private void SaveAttributeValuesInternal(int productId, IEnumerable<ProductAttributeValueInput> attributeValues, bool overwriteMissing)
        {
            if (attributeValues == null)
            {
                return;
            }

            var attributeList = attributeValues as IList<ProductAttributeValueInput> ?? attributeValues.ToList();

            if (attributeList.Count == 0 && !overwriteMissing)
            {
                return;
            }

            var existing = _productAttributeValueRepository.GetByProductId(productId)
                .ToDictionary(v => v.AttributeId);

            var processedAttributeIds = new HashSet<int>();

            foreach (var attributeValue in attributeList)
            {
                if (attributeValue == null)
                {
                    continue;
                }

                var attribute = _productAttributeRepository.GetById(attributeValue.AttributeId);
                if (attribute == null)
                {
                    Logger.Warning($"Attribute {attributeValue.AttributeId} not found when saving product attributes.");
                    continue;
                }

                var option = ResolveAttributeOption(attribute, attributeValue);

                if (!existing.TryGetValue(attributeValue.AttributeId, out var entity))
                {
                    entity = new ProductAttributeValue
                    {
                        ProductId = productId,
                        AttributeId = attributeValue.AttributeId
                    };
                    ApplyAttributeValue(attribute, attributeValue, entity, option);
                    _productAttributeValueRepository.Insert(entity);
                }
                else
                {
                    ApplyAttributeValue(attribute, attributeValue, entity, option);
                    _productAttributeValueRepository.Update(entity);
                }

                processedAttributeIds.Add(attributeValue.AttributeId);
            }

            if (overwriteMissing)
            {
                foreach (var unused in existing.Values.Where(v => !processedAttributeIds.Contains(v.AttributeId)))
                {
                    _productAttributeValueRepository.Delete(unused.Id);
                }
            }
        }

        private void ApplyAttributeValue(ProductAttribute attribute, ProductAttributeValueInput input, ProductAttributeValue entity, ProductAttributeOption? option)
        {
            entity.OptionId = option?.Id;
            entity.ValueText = null;
            entity.ValueNumber = null;
            entity.ValueDate = null;
            entity.ValueBool = null;

            if (option != null)
            {
                switch (attribute.DataType)
                {
                    case AttributeDataType.TEXT:
                        entity.ValueText = option.DisplayValue;
                        break;
                    case AttributeDataType.NUMBER:
                        if (input.NumberValue.HasValue)
                        {
                            entity.ValueNumber = input.NumberValue;
                        }
                        else if (!string.IsNullOrWhiteSpace(option.NormalizedValue)
                            && decimal.TryParse(option.NormalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var optionNumber))
                        {
                            entity.ValueNumber = optionNumber;
                        }

                        entity.ValueText = option.DisplayValue;
                        break;
                    case AttributeDataType.DATE:
                        if (input.DateValue.HasValue)
                        {
                            entity.ValueDate = input.DateValue;
                        }
                        else if (!string.IsNullOrWhiteSpace(option.NormalizedValue)
                            && DateTime.TryParse(option.NormalizedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var optionDate))
                        {
                            entity.ValueDate = optionDate;
                        }

                        entity.ValueText = option.DisplayValue;
                        break;
                    case AttributeDataType.BOOLEAN:
                        if (input.BoolValue.HasValue)
                        {
                            entity.ValueBool = input.BoolValue;
                        }
                        else if (!string.IsNullOrWhiteSpace(option.NormalizedValue))
                        {
                            if (bool.TryParse(option.NormalizedValue, out var boolValue))
                            {
                                entity.ValueBool = boolValue;
                            }
                            else if (int.TryParse(option.NormalizedValue, out var intValue))
                            {
                                entity.ValueBool = intValue != 0;
                            }
                        }

                        entity.ValueText = option.DisplayValue;
                        break;
                    default:
                        entity.ValueText = option.DisplayValue;
                        break;
                }

                return;
            }

            switch (attribute.DataType)
            {
                case AttributeDataType.TEXT:
                    entity.ValueText = input.TextValue ?? input.RawValue;
                    break;
                case AttributeDataType.NUMBER:
                    var number = input.NumberValue;
                    var numberRawProvided = !string.IsNullOrWhiteSpace(input.RawValue);
                    if (!number.HasValue && numberRawProvided)
                    {
                        if (decimal.TryParse(input.RawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                        {
                            number = parsed;
                        }
                    }

                    if (!number.HasValue)
                    {
                        if (numberRawProvided)
                        {
                            throw new FormatException($"Invalid number for attribute {attribute.Id}.");
                        }

                        entity.ValueNumber = null;
                        break;
                    }

                    entity.ValueNumber = number;
                    break;
                case AttributeDataType.DATE:
                    var date = input.DateValue;
                    var dateRawProvided = !string.IsNullOrWhiteSpace(input.RawValue);
                    if (!date.HasValue && dateRawProvided)
                    {
                        if (DateTime.TryParse(input.RawValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                        {
                            date = parsedDate;
                        }
                    }

                    if (!date.HasValue)
                    {
                        if (dateRawProvided)
                        {
                            throw new FormatException($"Invalid date for attribute {attribute.Id}.");
                        }

                        entity.ValueDate = null;
                        break;
                    }

                    entity.ValueDate = date;
                    break;
                case AttributeDataType.BOOLEAN:
                    var boolean = input.BoolValue;
                    var boolRawProvided = !string.IsNullOrWhiteSpace(input.RawValue);
                    if (!boolean.HasValue && boolRawProvided)
                    {
                        if (bool.TryParse(input.RawValue, out var parsedBool))
                        {
                            boolean = parsedBool;
                        }
                        else if (int.TryParse(input.RawValue, out var parsedInt))
                        {
                            boolean = parsedInt != 0;
                        }
                    }

                    if (!boolean.HasValue)
                    {
                        if (boolRawProvided)
                        {
                            throw new FormatException($"Invalid boolean for attribute {attribute.Id}.");
                        }

                        entity.ValueBool = null;
                        break;
                    }

                    entity.ValueBool = boolean;
                    break;
                default:
                    Logger.Warning($"Unhandled attribute data type {attribute.DataType} for attribute {attribute.Id}.");
                    break;
            }
        }

        private ProductAttributeOption? ResolveAttributeOption(ProductAttribute attribute, ProductAttributeValueInput input)
        {
            var options = GetOptionsForAttribute(attribute.Id);
            if (options.Count == 0)
            {
                input.OptionId = null;
                return null;
            }

            ProductAttributeOption? option = null;
            if (input.OptionId.HasValue)
            {
                option = options.FirstOrDefault(o => o.Id == input.OptionId.Value);
                if (option == null)
                {
                    throw new FormatException($"Option {input.OptionId.Value} not found for attribute {attribute.Id}.");
                }
            }

            if (option == null && !string.IsNullOrWhiteSpace(input.TextValue))
            {
                option = options.FirstOrDefault(o => string.Equals(o.DisplayValue, input.TextValue, StringComparison.OrdinalIgnoreCase));
            }

            if (option == null && !string.IsNullOrWhiteSpace(input.RawValue))
            {
                option = options.FirstOrDefault(o => string.Equals(o.DisplayValue, input.RawValue, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(o.NormalizedValue, input.RawValue, StringComparison.OrdinalIgnoreCase));
            }

            if (option == null)
            {
                if (input.OptionId.HasValue)
                {
                    throw new FormatException($"Option {input.OptionId.Value} not found for attribute {attribute.Id}.");
                }

                if (!string.IsNullOrWhiteSpace(input.RawValue) || !string.IsNullOrWhiteSpace(input.TextValue))
                {
                    throw new FormatException($"Value '{input.RawValue ?? input.TextValue}' is not valid for attribute {attribute.Id}.");
                }

                return null;
            }

            input.OptionId = option.Id;
            input.TextValue = option.DisplayValue;

            if (attribute.DataType == AttributeDataType.NUMBER)
            {
                if (!input.NumberValue.HasValue && !string.IsNullOrWhiteSpace(option.NormalizedValue)
                    && decimal.TryParse(option.NormalizedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                {
                    input.NumberValue = number;
                }
            }
            else if (attribute.DataType == AttributeDataType.DATE)
            {
                if (!input.DateValue.HasValue && !string.IsNullOrWhiteSpace(option.NormalizedValue)
                    && DateTime.TryParse(option.NormalizedValue, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                {
                    input.DateValue = date;
                }
            }
            else if (attribute.DataType == AttributeDataType.BOOLEAN)
            {
                if (!input.BoolValue.HasValue && !string.IsNullOrWhiteSpace(option.NormalizedValue))
                {
                    if (bool.TryParse(option.NormalizedValue, out var boolValue))
                    {
                        input.BoolValue = boolValue;
                    }
                    else if (int.TryParse(option.NormalizedValue, out var intValue))
                    {
                        input.BoolValue = intValue != 0;
                    }
                }
            }

            return option;
        }

        private IReadOnlyList<ProductAttributeOption> GetOptionsForAttribute(int attributeId)
        {
            if (_attributeOptionCache.TryGetValue(attributeId, out var cached))
            {
                return cached;
            }

            var options = _productAttributeOptionService.GetOptionsForAttribute(attributeId) ?? Array.Empty<ProductAttributeOption>();
            _attributeOptionCache[attributeId] = options;
            return options;
        }

        private bool ValidateProduct(Product product, bool isUpdate)
        {
            if (string.IsNullOrWhiteSpace(product.Sku))
            {
                Logger.Warning("Product SKU is required.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(product.Name))
            {
                Logger.Warning("Product name is required.");
                return false;
            }

            try
            {
                var existing = _productRepository.GetBySku(product.Sku);
                if (existing != null && (!isUpdate || existing.Id != product.Id))
                {
                    Logger.Warning($"SKU {product.Sku} already exists.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to validate SKU uniqueness", ex);
                return false;
            }

            try
            {
                _productCategoryRepository.GetById(product.CategoryId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Invalid category {product.CategoryId}", ex);
                return false;
            }

            if (product.BrandId.HasValue)
            {
                try
                {
                    _brandRepository.GetById(product.BrandId.Value);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Invalid brand {product.BrandId.Value}", ex);
                    return false;
                }
            }

            if (product.ModelId <= 0)
            {
                Logger.Warning("Product model is required.");
                return false;
            }

            try
            {
                var model = _productModelRepository.GetById(product.ModelId);
                _modelNameCache[model.Id] = model.Name ?? string.Empty;
            }
            catch (Exception ex)
            {
                Logger.Error($"Invalid model {product.ModelId}", ex);
                return false;
            }

            return true;
        }

        private void EnsureSeedData()
        {
            if (_seedAttempted)
            {
                return;
            }

            _seedAttempted = true;

            try
            {
                EnsureSeedModels();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to seed product models", ex);
            }

            try
            {
                EnsureSeedAttributeOptions();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to seed product attribute options", ex);
            }
        }

        private void EnsureSeedModels()
        {
            var existing = _productModelRepository.GetAll()?.ToList() ?? new List<ProductModel>();
            foreach (var model in existing)
            {
                _modelNameCache[model.Id] = model.Name ?? string.Empty;
            }

            if (existing.Count > 0)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var seeds = new List<ProductModel>
            {
                new ProductModel
                {
                    Name = "iPhone 15 Pro Max",
                    Slug = GenerateSlug("iPhone 15 Pro Max"),
                    Description = "Apple flagship with A17 Pro chipset",
                    DefaultImageUrl = "https://example.com/images/iphone-15-pro-max.png",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ProductModel
                {
                    Name = "Samsung Galaxy S23 Ultra",
                    Slug = GenerateSlug("Samsung Galaxy S23 Ultra"),
                    Description = "Samsung premium S series with S Pen",
                    DefaultImageUrl = "https://example.com/images/galaxy-s23-ultra.png",
                    CreatedAt = now,
                    UpdatedAt = now
                },
                new ProductModel
                {
                    Name = "Xiaomi 13 Pro",
                    Slug = GenerateSlug("Xiaomi 13 Pro"),
                    Description = "Xiaomi flagship co-engineered with Leica",
                    DefaultImageUrl = "https://example.com/images/xiaomi-13-pro.png",
                    CreatedAt = now,
                    UpdatedAt = now
                }
            };

            foreach (var seed in seeds)
            {
                try
                {
                    var existingModel = _productModelRepository.FindBySlug(seed.Slug);
                    if (existingModel != null)
                    {
                        _modelNameCache[existingModel.Id] = existingModel.Name ?? string.Empty;
                        continue;
                    }

                    _productModelRepository.Insert(seed);
                    _modelNameCache[seed.Id] = seed.Name ?? string.Empty;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Failed to insert product model seed {seed.Name}", ex);
                }
            }
        }

        private void EnsureSeedAttributeOptions()
        {
            var attributeSeeds = new Dictionary<string, (string Display, string Normalized, int SortOrder)[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["RAM"] = new[]
                {
                    ("4 GB", "4", 10),
                    ("8 GB", "8", 20),
                    ("12 GB", "12", 30),
                    ("16 GB", "16", 40)
                },
                ["Storage"] = new[]
                {
                    ("64 GB", "64", 10),
                    ("128 GB", "128", 20),
                    ("256 GB", "256", 30),
                    ("512 GB", "512", 40)
                },
                ["ROM"] = new[]
                {
                    ("64 GB", "64", 10),
                    ("128 GB", "128", 20),
                    ("256 GB", "256", 30),
                    ("512 GB", "512", 40)
                },
                ["Color"] = new[]
                {
                    ("Black", "black", 10),
                    ("Silver", "silver", 20),
                    ("Blue", "blue", 30),
                    ("Gold", "gold", 40)
                }
            };

            var attributes = _productAttributeRepository.GetByNames(attributeSeeds.Keys);
            foreach (var pair in attributes)
            {
                if (!attributeSeeds.TryGetValue(pair.Key, out var seeds))
                {
                    continue;
                }

                foreach (var seed in seeds)
                {
                    _productAttributeOptionService.EnsureOption(pair.Value.Id, seed.Display, seed.Normalized, seed.SortOrder);
                }

                _attributeOptionCache.Remove(pair.Value.Id);
                GetOptionsForAttribute(pair.Value.Id);
            }
        }

        private static string GenerateSlug(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text.Trim().ToLowerInvariant();
            var chars = normalized.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
            var slug = new string(chars);
            while (slug.Contains("--", StringComparison.Ordinal))
            {
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            }

            return slug.Trim('-');
        }

        private string EnsureUniqueModelSlug(string slug, int currentId)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = Guid.NewGuid().ToString("N");
            }

            var uniqueSlug = slug;
            var counter = 1;

            while (true)
            {
                var existing = _productModelRepository.FindBySlug(uniqueSlug);
                if (existing == null || existing.Id == currentId)
                {
                    break;
                }

                uniqueSlug = $"{slug}-{counter}";
                counter++;
            }

            return uniqueSlug;
        }

        private void SynchronizeSerialTracking(Product product, IEnumerable<ProductSerial>? serials)
        {
            try
            {
                var serialList = serials?.ToList() ?? _productSerialRepository.GetByProductId(product.Id).ToList();
                var hasSerials = serialList.Any();

                if (hasSerials && !product.IsSerialTracked)
                {
                    Logger.Warning($"Product {product.Id} has serials but IsSerialTracked is false. Updating flag.");
                    product.IsSerialTracked = true;
                    _productRepository.Update(product);
                }
                else if (!hasSerials && product.IsSerialTracked)
                {
                    Logger.Warning($"Product {product.Id} marked as serial tracked but no serials exist.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to synchronize serial tracking for product {product?.Id}", ex);
            }
        }

        #endregion
    }
}
