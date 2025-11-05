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

        public ProductService(
            IProductRepository productRepository,
            IBrandRepository brandRepository,
            IProductCategoryRepository productCategoryRepository,
            IProductAttributeRepository productAttributeRepository,
            IProductAttributeValueRepository productAttributeValueRepository,
            IProductSerialRepository productSerialRepository)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
            _productCategoryRepository = productCategoryRepository ?? throw new ArgumentNullException(nameof(productCategoryRepository));
            _productAttributeRepository = productAttributeRepository ?? throw new ArgumentNullException(nameof(productAttributeRepository));
            _productAttributeValueRepository = productAttributeValueRepository ?? throw new ArgumentNullException(nameof(productAttributeValueRepository));
            _productSerialRepository = productSerialRepository ?? throw new ArgumentNullException(nameof(productSerialRepository));
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

                        return new ProductListItemViewModel(p, categoryName, brandName, serialCount);
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

                var productViewModel = new ProductListItemViewModel(product, categoryName, brandName, serialViewModels.Count);

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

        private ProductAttributeValueViewModel? BuildAttributeViewModel(ProductAttributeValue value)
        {
            try
            {
                var attribute = _productAttributeRepository.GetById(value.AttributeId);
                if (attribute == null)
                {
                    return null;
                }

                return new ProductAttributeValueViewModel
                {
                    AttributeId = value.AttributeId,
                    AttributeName = attribute.Name,
                    DataType = attribute.DataType,
                    ValueText = value.ValueText,
                    ValueNumber = value.ValueNumber,
                    ValueDate = value.ValueDate,
                    ValueBool = value.ValueBool
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

                    ApplyAttributeValue(attribute, attributeValue, dummy);
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

                if (!existing.TryGetValue(attributeValue.AttributeId, out var entity))
                {
                    entity = new ProductAttributeValue
                    {
                        ProductId = productId,
                        AttributeId = attributeValue.AttributeId
                    };
                    ApplyAttributeValue(attribute, attributeValue, entity);
                    _productAttributeValueRepository.Insert(entity);
                }
                else
                {
                    ApplyAttributeValue(attribute, attributeValue, entity);
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

        private void ApplyAttributeValue(ProductAttribute attribute, ProductAttributeValueInput input, ProductAttributeValue entity)
        {
            entity.ValueText = null;
            entity.ValueNumber = null;
            entity.ValueDate = null;
            entity.ValueBool = null;

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

            return true;
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
