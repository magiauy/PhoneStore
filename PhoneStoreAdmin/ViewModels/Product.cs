using PhoneStoreAdmin.Helpers;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Models.Enums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class ProductListItemViewModel
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
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

        public ProductListItemViewModel(Product product, string categoryName, string? brandName, int serialCount = 0)
        {
            Id = product.Id;
            Sku = product.Sku;
            Name = product.Name;
            CategoryId = product.CategoryId;
            CategoryName = categoryName;
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

    public class ProductAttributeValueViewModel
    {
        public int AttributeId { get; set; }
        public string AttributeName { get; set; } = string.Empty;
        public AttributeDataType DataType { get; set; }
        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
        public DateTime? ValueDate { get; set; }
        public bool? ValueBool { get; set; }

        public string DisplayValue
        {
            get
            {
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
        public string? TextValue { get; set; }
        public decimal? NumberValue { get; set; }
        public DateTime? DateValue { get; set; }
        public bool? BoolValue { get; set; }
        public string? RawValue { get; set; }
    }
}
