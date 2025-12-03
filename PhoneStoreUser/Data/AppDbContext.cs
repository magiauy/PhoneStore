using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace PhoneStoreUser.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BrandEntity> Brands => Set<BrandEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductModelEntity> ProductModels => Set<ProductModelEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<PersonEntity> Persons => Set<PersonEntity>();
    public DbSet<PersonEntity> People => Set<PersonEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<ProductAttributeEntity> ProductAttributes => Set<ProductAttributeEntity>();
    public DbSet<ProductAttributeValueEntity> ProductAttributeValues => Set<ProductAttributeValueEntity>();
    public DbSet<ProductAttributeOptionEntity> ProductAttributeOptions => Set<ProductAttributeOptionEntity>();
    public DbSet<ProductModelAttributeEntity> ProductModelAttributes => Set<ProductModelAttributeEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();
    public DbSet<ProductSerialEntity> ProductSerials => Set<ProductSerialEntity>();
    public DbSet<InvoiceLineSerialEntity> InvoiceLineSerials => Set<InvoiceLineSerialEntity>();
    public DbSet<SupplierEntity> Suppliers => Set<SupplierEntity>();
    public DbSet<BatchEntity> Batches => Set<BatchEntity>();
    public DbSet<BatchProductEntity> BatchProducts => Set<BatchProductEntity>();
    public DbSet<PurchaseOrderEntity> PurchaseOrders => Set<PurchaseOrderEntity>();
    public DbSet<PurchaseOrderLineEntity> PurchaseOrderLines => Set<PurchaseOrderLineEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<PromotionEntity> Promotions => Set<PromotionEntity>();
    public DbSet<PromotionCodeEntity> PromotionCodes => Set<PromotionCodeEntity>();
    public DbSet<PricingAlertEntity> PricingAlerts => Set<PricingAlertEntity>();
    public DbSet<PricingHistoryEntity> PricingHistories => Set<PricingHistoryEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PromotionEntity>(entity =>
        {
            entity.ToTable("promotions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<PromotionCodeEntity>(entity =>
        {
            entity.ToTable("promotion_codes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");
            entity.Property(e => e.MinimumAmount).HasColumnName("minimum_amount");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.UsedCount).HasColumnName("used_count");
            entity.Property(e => e.IsActive).HasColumnName("is_active");

            entity.HasOne(e => e.Promotion).WithMany().HasForeignKey(e => e.PromotionId);
        });

        modelBuilder.Entity<ReviewEntity>(entity =>
        {
            entity.ToTable("product_reviews");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Comment).HasColumnName("comment").HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");

            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.Person).WithMany().HasForeignKey(e => e.PersonId);
        });

        modelBuilder.Entity<BrandEntity>(entity =>
        {
            entity.ToTable("brands");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
        });

        modelBuilder.Entity<ProductEntity>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Sku).HasColumnName("sku").HasMaxLength(100);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.BrandId).HasColumnName("brand_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.Cost).HasColumnName("cost");
            entity.Property(e => e.IsSerialTracked).HasColumnName("is_serial_tracked");
            entity.Property(e => e.WarrantyMonths).HasColumnName("warranty_months");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            // Dynamic Pricing fields
            entity.Property(e => e.CostFifo).HasColumnName("cost_fifo").HasDefaultValue(0);
            entity.Property(e => e.CostNifo).HasColumnName("cost_nifo").HasDefaultValue(0);
            entity.Property(e => e.MarketTrend).HasColumnName("market_trend").HasDefaultValue(0);
            entity.Property(e => e.PricingMode).HasColumnName("pricing_mode").HasDefaultValue(0);
            entity.Property(e => e.PriceUpdatedAt).HasColumnName("price_updated_at");
        });

        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.ToTable("product_categories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(120);
            entity.Property(e => e.ParentId).HasColumnName("parent_id");
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(255);
        });

        modelBuilder.Entity<PersonEntity>(entity =>
        {
            entity.ToTable("persons");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Code).HasColumnName("code").HasMaxLength(30);
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.PersonType).HasColumnName("person_type").HasMaxLength(30);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<CustomerEntity>(entity =>
        {
            entity.ToTable("customers");
            entity.HasKey(e => e.PersonId);
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
        });

        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.ToTable("accounts");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.Password).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<ProductAttributeEntity>(entity =>
        {
            entity.ToTable("product_attributes");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(80);
            entity.Property(e => e.DataType).HasColumnName("data_type").HasMaxLength(50);
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(255);
        });

        modelBuilder.Entity<ProductAttributeValueEntity>(entity =>
        {
            entity.ToTable("product_attribute_values");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.AttributeId).HasColumnName("attribute_id");
            entity.Property(e => e.OptionId).HasColumnName("option_id");
            entity.Property(e => e.ValueText).HasColumnName("value_text").HasMaxLength(255);
            entity.Property(e => e.ValueNumber).HasColumnName("value_number");
            entity.Property(e => e.ValueDate).HasColumnName("value_date");
            entity.Property(e => e.ValueBool).HasColumnName("value_bool");
        });

        modelBuilder.Entity<ProductAttributeOptionEntity>(entity =>
        {
            entity.ToTable("product_attribute_options");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttributeId).HasColumnName("attribute_id");
            entity.Property(e => e.DisplayValue).HasColumnName("display_value").HasMaxLength(50);
            entity.Property(e => e.NormalizedValue).HasColumnName("normalized_value");
            entity.Property(e => e.SortOrder).HasColumnName("sort_order");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductModelEntity>(entity =>
        {
            entity.ToTable("product_models");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.Slug).HasColumnName("slug").HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
            entity.Property(e => e.DefaultImageUrl).HasColumnName("default_image_url").HasMaxLength(255);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<ProductModelAttributeEntity>(entity =>
        {
            entity.ToTable("product_model_attributes");
            entity.HasKey(e => new { e.ModelId, e.AttributeId });
            entity.Property(e => e.ModelId).HasColumnName("model_id");
            entity.Property(e => e.AttributeId).HasColumnName("attribute_id");
        });

        modelBuilder.Entity<InvoiceEntity>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.PromotionCodeId).HasColumnName("promotion_code_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.InvoiceDate).HasColumnName("invoice_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.DiscountAmount).HasColumnName("discount_amount");
            entity.Property(e => e.FinalAmount).HasColumnName("final_amount");
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method");
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(255);

            entity.HasOne<PersonEntity>().WithMany().HasForeignKey(e => e.PersonId);
            entity.HasOne<PersonEntity>().WithMany().HasForeignKey(e => e.CreatedBy);
            entity.HasMany(e => e.Lines).WithOne(l => l.Invoice).HasForeignKey(l => l.InvoiceId);
        });

        modelBuilder.Entity<InvoiceLineEntity>(entity =>
        {
            entity.ToTable("invoice_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InvoiceId).HasColumnName("invoice_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitPrice).HasColumnName("unit_price");
            entity.Property(e => e.DiscountPct).HasColumnName("discount_pct");
            entity.Property(e => e.TotalPrice).HasColumnName("total_price");

            entity.HasOne(e => e.Invoice).WithMany(e => e.Lines).HasForeignKey(e => e.InvoiceId);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasMany(e => e.LineSerials).WithOne(ls => ls.InvoiceLine).HasForeignKey(ls => ls.InvoiceLineId);
        });

        modelBuilder.Entity<ProductSerialEntity>(entity =>
        {
            entity.ToTable("product_serials");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.SerialNumber).HasColumnName("serial_number").HasMaxLength(100);
            entity.Property(e => e.Imei1).HasColumnName("imei1").HasMaxLength(20);
            entity.Property(e => e.Imei2).HasColumnName("imei2").HasMaxLength(20);
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(50);
            entity.Property(e => e.PurchaseOrderLineId).HasColumnName("purchase_order_line_id");
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(255);

            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<InvoiceLineSerialEntity>(entity =>
        {
            entity.ToTable("invoice_line_serials");
            entity.HasKey(e => new { e.InvoiceLineId, e.ProductSerialId });
            entity.Property(e => e.InvoiceLineId).HasColumnName("invoice_line_id");
            entity.Property(e => e.ProductSerialId).HasColumnName("product_serial_id");

            entity.HasOne(e => e.InvoiceLine).WithMany(il => il.LineSerials).HasForeignKey(e => e.InvoiceLineId);
            entity.HasOne(e => e.ProductSerial).WithMany().HasForeignKey(e => e.ProductSerialId);
        });

        modelBuilder.Entity<SupplierEntity>(entity =>
        {
            entity.ToTable("suppliers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(160);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(120);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(255);
            entity.Property(e => e.TaxNumber).HasColumnName("tax_number").HasMaxLength(50);
            entity.Property(e => e.IsActive).HasColumnName("is_active");
        });

        modelBuilder.Entity<PurchaseOrderEntity>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SupplierId).HasColumnName("supplier_id");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.OrderDate).HasColumnName("order_date");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount");
            entity.Property(e => e.Note).HasColumnName("note");

            entity.HasOne(e => e.Supplier).WithMany().HasForeignKey(e => e.SupplierId);
            entity.HasMany(e => e.Lines).WithOne(l => l.PurchaseOrder).HasForeignKey(l => l.PurchaseOrderId);
            entity.HasMany(e => e.Batches).WithOne(b => b.PurchaseOrder).HasForeignKey(b => b.PurchaseOrderId);
        });

        modelBuilder.Entity<PurchaseOrderLineEntity>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.UnitCost).HasColumnName("unit_cost");
            entity.Property(e => e.TotalCost).HasColumnName("total_cost");

            entity.HasOne(e => e.PurchaseOrder).WithMany(po => po.Lines).HasForeignKey(e => e.PurchaseOrderId);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<BatchEntity>(entity =>
        {
            entity.ToTable("batches");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.PurchaseOrderId).HasColumnName("purchase_order_id");
            entity.Property(e => e.BatchCode).HasColumnName("batch_code").HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.Note).HasColumnName("note").HasMaxLength(255);

            entity.HasOne(e => e.PurchaseOrder).WithMany(po => po.Batches).HasForeignKey(e => e.PurchaseOrderId);
        });

        modelBuilder.Entity<BatchProductEntity>(entity =>
        {
            entity.ToTable("batch_products");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatchId).HasColumnName("batch_id");
            entity.Property(e => e.ProductId).HasColumnName("product_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.CostPrice).HasColumnName("cost_price");
            entity.Property(e => e.SellingPrice).HasColumnName("selling_price");

            entity.HasOne(e => e.Batch).WithMany(b => b.BatchProducts).HasForeignKey(e => e.BatchId);
            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<PricingAlertEntity>(entity =>
        {
            entity.ToTable("pricing_alert");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.VariancePercent).HasColumnName("variance_percent").HasColumnType("decimal(5,2)").IsRequired();
            entity.Property(e => e.CostFifoSnapshot).HasColumnName("cost_fifo_snapshot").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.CostNifoSnapshot).HasColumnName("cost_nifo_snapshot").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.CurrentStock).HasColumnName("current_stock").IsRequired();
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(0);
            entity.Property(e => e.ResolvedBy).HasColumnName("resolved_by");
            entity.Property(e => e.ResolvedAt).HasColumnName("resolved_at");
            entity.Property(e => e.ResolvedNote).HasColumnName("resolved_note").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.ResolvedByAccount).WithMany().HasForeignKey(e => e.ResolvedBy);
        });

        modelBuilder.Entity<PricingHistoryEntity>(entity =>
        {
            entity.ToTable("pricing_history");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProductId).HasColumnName("product_id").IsRequired();
            entity.Property(e => e.OldPrice).HasColumnName("old_price").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.NewPrice).HasColumnName("new_price").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.CostFifo).HasColumnName("cost_fifo").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.CostNifo).HasColumnName("cost_nifo").HasColumnType("decimal(12,2)").IsRequired();
            entity.Property(e => e.ChangeReason).HasColumnName("change_reason").HasMaxLength(50).IsRequired();
            entity.Property(e => e.ChangedBy).HasColumnName("changed_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.Product).WithMany().HasForeignKey(e => e.ProductId);
            entity.HasOne(e => e.ChangedByAccount).WithMany().HasForeignKey(e => e.ChangedBy);
        });
    }
}

// Entities Definitions

public class BrandEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProductEntity
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public int ModelId { get; set; }
    public int? BrandId { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public bool IsSerialTracked { get; set; }
    public int WarrantyMonths { get; set; }
    public string Status { get; set; } = "active";
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Dynamic Pricing fields
    public decimal CostFifo { get; set; }
    public decimal CostNifo { get; set; }
    public int MarketTrend { get; set; } // 0=STABLE, 1=UP, 2=DOWN
    public int PricingMode { get; set; } // 0=AUTO_PROTECT, 1=CLEARANCE
    public DateTime? PriceUpdatedAt { get; set; }

    public CategoryEntity? Category { get; set; }
    public BrandEntity? Brand { get; set; }
    public ProductModelEntity? Model { get; set; }
}

public class ProductModelEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? DefaultImageUrl { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CategoryEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int? ParentId { get; set; }
    public string? Note { get; set; }
}

public class PersonEntity
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string PersonType { get; set; } = "CUSTOMER";
    public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;
    public bool? IsActive { get; set; } = true;
}

public class CustomerEntity
{
    public int PersonId { get; set; }
    public string? Address { get; set; }

    public DateTime? LastOrderDate { get; set; }
    public decimal? TotalSpend { get; set; }
}

public class AccountEntity
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public DateTime? LastLogin { get; set; }
    public int PersonId { get; set; }
    public bool IsActive { get; set; }
}

public class ProductAttributeEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class ProductAttributeValueEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int AttributeId { get; set; }
    public int? OptionId { get; set; }
    public string? ValueText { get; set; }
    public decimal? ValueNumber { get; set; }
    public DateTime? ValueDate { get; set; }
    public bool? ValueBool { get; set; }
}

public class ProductAttributeOptionEntity
{
    public int Id { get; set; }
    public int AttributeId { get; set; }
    public string DisplayValue { get; set; } = string.Empty;
    public int? NormalizedValue { get; set; }
    public int SortOrder { get; set; }
    public int? IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductModelAttributeEntity
{
    public int ModelId { get; set; }
    public int AttributeId { get; set; }
}
public class InvoiceEntity
{
    public int Id { get; set; }
    public int? PersonId { get; set; }
    public int? PromotionCodeId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public string Status { get; set; } = "unpaid";
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string PaymentMethod { get; set; } = "cash";
    public string? Note { get; set; }

    public List<InvoiceLineEntity> Lines { get; set; } = new();
}

public class InvoiceLineEntity
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountPct { get; set; }
    public decimal TotalPrice { get; set; }

    public InvoiceEntity? Invoice { get; set; }
    public ProductEntity? Product { get; set; }
    public List<InvoiceLineSerialEntity> LineSerials { get; set; } = new();
}

public class ProductSerialEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string? SerialNumber { get; set; }
    public string? Imei1 { get; set; }
    public string? Imei2 { get; set; }
    public int? BatchId { get; set; }
    public string Status { get; set; } = "in_stock";
    public int? PurchaseOrderLineId { get; set; }
    public string? Note { get; set; }

    public ProductEntity? Product { get; set; }
}

public class InvoiceLineSerialEntity
{
    public int InvoiceLineId { get; set; }
    public int ProductSerialId { get; set; }

    public InvoiceLineEntity? InvoiceLine { get; set; }
    public ProductSerialEntity? ProductSerial { get; set; }
}

public class SupplierEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ReviewEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int PersonId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductEntity? Product { get; set; }
    public PersonEntity? Person { get; set; }
}

public class PurchaseOrderEntity
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "DRAFT";
    public decimal TotalAmount { get; set; }
    public string? Note { get; set; }

    public SupplierEntity? Supplier { get; set; }
    public List<PurchaseOrderLineEntity> Lines { get; set; } = new();
    public List<BatchEntity> Batches { get; set; } = new();
}

public class PurchaseOrderLineEntity
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public PurchaseOrderEntity? PurchaseOrder { get; set; }
    public ProductEntity? Product { get; set; }
}

public class BatchEntity
{
    public int Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? Note { get; set; }

    public PurchaseOrderEntity? PurchaseOrder { get; set; }
    public List<BatchProductEntity> BatchProducts { get; set; } = new();
}

public class BatchProductEntity
{
    public int Id { get; set; }
    public int BatchId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal SellingPrice { get; set; }

    public BatchEntity? Batch { get; set; }
    public ProductEntity? Product { get; set; }
}

public class PromotionEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PromotionCodeEntity
{
    public int Id { get; set; }
    public int PromotionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public PromotionEntity? Promotion { get; set; }
}

/// <summary>
/// Entity cho cảnh báo rủi ro tồn kho khi thị trường giảm giá
/// </summary>
public class PricingAlertEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal CostFifoSnapshot { get; set; }
    public decimal CostNifoSnapshot { get; set; }
    public int CurrentStock { get; set; }
    public int Status { get; set; } // 0=PENDING, 1=RESOLVED_HOLD, 2=RESOLVED_CLEARANCE
    public int? ResolvedBy { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductEntity? Product { get; set; }
    public AccountEntity? ResolvedByAccount { get; set; }
}

/// <summary>
/// Entity cho lịch sử biến động giá sản phẩm
/// </summary>
public class PricingHistoryEntity
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public decimal CostFifo { get; set; }
    public decimal CostNifo { get; set; }
    public string ChangeReason { get; set; } = string.Empty; // AUTO_INCREASE, CLEARANCE, MANUAL
    public int? ChangedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ProductEntity? Product { get; set; }
    public AccountEntity? ChangedByAccount { get; set; }
}
