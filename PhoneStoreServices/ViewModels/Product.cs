using PhoneStore.Services.Helpers;
using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoneStore.Services.ViewModels
{
    public class ProductListItemViewModel
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ModelId { get; set; }
        public string ModelName { get; set; } = string.Empty;
        public int? BrandId { get; set; }
        public string? BrandName { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public bool IsSerialTracked { get; set; }
        public int WarrantyMonths { get; set; }
        public ProductStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SerialCount { get; set; }

        public string StatusText
        {
            get
            {
                var key = Status switch
                {
                    ProductStatus.ACTIVE => "Status_Active",
                    ProductStatus.INACTIVE => "Status_Inactive",
                    ProductStatus.DISCONTINUED => "Product_Status_Discontinued",
                    _ => "Status_Active"
                };

                return LocalizationHelper.GetString(key);
            }
        }

        public ProductListItemViewModel()
        {
        }

        public ProductListItemViewModel(Product product, string categoryName, string? brandName, int serialCount = 0, string? modelName = null)
        {
            Id = product.Id;
            Sku = product.Sku;
            Name = product.Name;
            CategoryId = product.CategoryId;
            CategoryName = categoryName;
            ModelId = product.ModelId;
            ModelName = modelName ?? string.Empty;
            BrandId = product.BrandId;
            BrandName = brandName;
            Price = product.Price;
            Cost = product.Cost;
            IsSerialTracked = product.IsSerialTracked;
            WarrantyMonths = product.WarrantyMonths;
            Status = product.Status;
            CreatedAt = product.CreatedAt;
            SerialCount = serialCount;
        }
    }

    public class ProductModelListItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? DefaultImageUrl { get; set; }
        public int VariantCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string DescriptionPreview
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Description))
                {
                    return string.Empty;
                }

                var trimmed = Description.Trim();
                if (trimmed.Length <= 160)
                {
                    return trimmed;
                }

                return trimmed.Substring(0, 157) + "...";
            }
        }
    }

    public class ProductModelDetailViewModel
    {
        public ProductModelDetailViewModel(
            ProductModelListItemViewModel model,
            IEnumerable<ProductListItemViewModel> variants,
            IEnumerable<ProductAttribute> attributes)
        {
            Model = model;
            Variants = variants?.ToList() ?? new List<ProductListItemViewModel>();
            Attributes = attributes?.ToList() ?? new List<ProductAttribute>();
        }

        public ProductModelListItemViewModel Model { get; }
        public IReadOnlyList<ProductListItemViewModel> Variants { get; }
        public IReadOnlyList<ProductAttribute> Attributes { get; }

        public bool HasVariants => Variants.Count > 0;
        public bool HasAttributes => Attributes.Count > 0;
    }

    public class ProductAttributeValueViewModel
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public AttributeDataType DataType { get; set; }
        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBool { get; set; }
        public int? OptionId { get; set; }
        public string? OptionDisplayValue { get; set; }

        public string DisplayValue
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(OptionDisplayValue))
                {
                    return OptionDisplayValue;
                }

                return DataType switch
                {
                    AttributeDataType.TEXT => ValueText ?? string.Empty,
                    AttributeDataType.NUMBER => ValueNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                    AttributeDataType.DATE => ValueDate?.ToString("yyyy-MM-dd") ?? string.Empty,
                    AttributeDataType.BOOLEAN => ValueBool.HasValue
                        ? (ValueBool.Value ? LocalizationHelper.GetString("Status_Active") : LocalizationHelper.GetString("Status_Inactive"))
                        : string.Empty,
                    _ => string.Empty
                };
            }
        }
    }

    public class ProductSerialViewModel
    {
        public int Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? Imei1 { get; set; }
        public string? Imei2 { get; set; }
        public int? BatchId { get; set; }
        public SerialStatus Status { get; set; }
        public int? PurchaseOrderLineId { get; set; }
        public string? Note { get; set; }

        public string StatusText
        {
            get
            {
                var key = $"SerialStatus_{Status}";
                return LocalizationHelper.GetString(key);
            }
        }
    }

    public class ProductDetailViewModel
    {
        public ProductListItemViewModel Product { get; }
        public IReadOnlyList<ProductAttributeValueViewModel> AttributeValues { get; }
        public IReadOnlyList<ProductSerialViewModel> Serials { get; }

        public ProductDetailViewModel(
            ProductListItemViewModel product,
            IEnumerable<ProductAttributeValueViewModel> attributes,
            IEnumerable<ProductSerialViewModel> serials)
        {
            Product = product;
            AttributeValues = attributes?.ToList() ?? new List<ProductAttributeValueViewModel>();
            Serials = serials?.ToList() ?? new List<ProductSerialViewModel>();
        }
    }

    public class ProductBatchDetailViewModel
    {
        public ProductBatchDetailViewModel(
            int batchProductId,
            int? batchId,
            int quantity,
            decimal costPrice,
            decimal sellingPrice,
            IEnumerable<ProductSerialViewModel>? serials,
            bool isVirtualBatch = false)
        {
            BatchProductId = batchProductId;
            BatchId = batchId;
            Quantity = quantity;
            CostPrice = costPrice;
            SellingPrice = sellingPrice;
            Serials = serials?.ToList() ?? new List<ProductSerialViewModel>();
            IsVirtualBatch = isVirtualBatch;
        }

        public int BatchProductId { get; }
        public int? BatchId { get; }
        public int Quantity { get; }
        public decimal CostPrice { get; }
        public decimal SellingPrice { get; }
        public IReadOnlyList<ProductSerialViewModel> Serials { get; }
        public bool IsVirtualBatch { get; }

        public int SerialCount => Serials.Count;

        public string DisplayName
        {
            get
            {
                if (IsVirtualBatch)
                {
                    return LocalizationHelper.GetString("ProductManagement_UnassignedBatchName");
                }

                var format = LocalizationHelper.GetString("ProductManagement_BatchDisplayName");
                return string.Format(CultureInfo.CurrentCulture, format, BatchId);
            }
        }

        public bool HasSerials => SerialCount > 0;
    }

    public class ProductManagementDetailViewModel
    {
        public ProductManagementDetailViewModel(
            ProductListItemViewModel product,
            IEnumerable<ProductBatchDetailViewModel>? batches,
            IEnumerable<ProductSerialViewModel>? unassignedSerials)
        {
            Product = product;

            var batchList = batches?.ToList() ?? new List<ProductBatchDetailViewModel>();
            var unassignedList = unassignedSerials?.ToList() ?? new List<ProductSerialViewModel>();

            Batches = batchList;
            UnassignedSerials = unassignedList;
            AllSerials = batchList.SelectMany(b => b.Serials).Concat(unassignedList).ToList();

            var actualBatches = batchList.Where(b => !b.IsVirtualBatch).ToList();
            TotalQuantity = actualBatches.Sum(b => b.Quantity);
            AverageCostPrice = actualBatches.Count > 0 ? actualBatches.Average(b => b.CostPrice) : 0m;
            AverageSellingPrice = actualBatches.Count > 0 ? actualBatches.Average(b => b.SellingPrice) : 0m;
            TotalSerials = AllSerials.Count;
        }

        public ProductListItemViewModel Product { get; }
        public IReadOnlyList<ProductBatchDetailViewModel> Batches { get; }
        public IReadOnlyList<ProductSerialViewModel> UnassignedSerials { get; }
        public IReadOnlyList<ProductSerialViewModel> AllSerials { get; }

        public int TotalQuantity { get; }
        public decimal AverageCostPrice { get; }
        public decimal AverageSellingPrice { get; }
        public int TotalSerials { get; }

        public bool HasBatches => Batches.Any(b => !b.IsVirtualBatch);
        public bool HasSerials => TotalSerials > 0;
    }

    public class ProductFilterCriteria
    {
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public ProductStatus? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public void Normalize()
        {
            if (Page <= 0)
            {
                Page = 1;
            }

            if (PageSize <= 0)
            {
                PageSize = 20;
            }

            Sku = string.IsNullOrWhiteSpace(Sku) ? null : Sku.Trim();
            Name = string.IsNullOrWhiteSpace(Name) ? null : Name.Trim();
        }
    }

    public class ProductSearchResult
    {
        public IEnumerable<ProductListItemViewModel> Products { get; set; }
        public InfoTable Info { get; set; }

        public ProductSearchResult(IEnumerable<ProductListItemViewModel> products, InfoTable info)
        {
            Products = products;
            Info = info;
        }
    }

    public class ProductAttributeValueInput
    {
        public int AttributeId { get; set; }
        public int? OptionId { get; set; }
        public string? TextValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
        public string? RawValue { get; set; }
    }

    public class ProductAttributeDefinition
    {
        public ProductAttributeDefinition(ProductAttribute attribute, IReadOnlyList<ProductAttributeOption> options)
        {
            Attribute = attribute;
            Options = options ?? Array.Empty<ProductAttributeOption>();
        }

        public ProductAttribute Attribute { get; }
        public IReadOnlyList<ProductAttributeOption> Options { get; }
    }

    public class ProductAttributeSelectionViewModel
    {
        public ProductAttributeSelectionViewModel(ProductAttributeDefinition definition)
        {
            Definition = definition;
            Attribute = definition.Attribute;
            Options = definition.Options ?? Array.Empty<ProductAttributeOption>();
        }

        public ProductAttributeDefinition Definition { get; }
        public ProductAttribute Attribute { get; }
        public IReadOnlyList<ProductAttributeOption> Options { get; }

        public bool IsSelected { get; set; }
        public ProductAttributeOption? SelectedOption { get; set; }
        public string? TextValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }

        public void Apply(ProductAttributeValueViewModel value)
        {
            if (value == null)
            {
                return;
            }

            IsSelected = true;
            SelectedOption = value.OptionId.HasValue
                ? Options.FirstOrDefault(o => o.Id == value.OptionId.Value)
                : null;
            TextValue = value.ValueText;
            NumberValue = value.ValueNumber;
            DateValue = value.ValueDate;
            BoolValue = value.ValueBool;
        }
    }
}
