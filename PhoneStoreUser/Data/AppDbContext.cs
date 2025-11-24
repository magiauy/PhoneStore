using Microsoft.EntityFrameworkCore;

namespace PhoneStoreUser.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BrandEntity> Brands => Set<BrandEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<ProductModelEntity> ProductModels => Set<ProductModelEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<PersonEntity> People => Set<PersonEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<ProductAttributeEntity> ProductAttributes => Set<ProductAttributeEntity>();
    public DbSet<ProductAttributeValueEntity> ProductAttributeValues => Set<ProductAttributeValueEntity>();
    public DbSet<ProductAttributeOptionEntity> ProductAttributeOptions => Set<ProductAttributeOptionEntity>();
    public DbSet<ProductModelAttributeEntity> ProductModelAttributes => Set<ProductModelAttributeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(255);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(255);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
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
    }
}

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
    public DateTime CreatedAt { get; set; }
}

public class ProductModelEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DefaultImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Note { get; set; }
}

public class PersonEntity
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class CustomerEntity
{
    public int PersonId { get; set; }
    public string? Address { get; set; }
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
    public int NormalizedValue { get; set; }
    public int SortOrder { get; set; }
    public int IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ProductModelAttributeEntity
{
    public int ModelId { get; set; }
    public int AttributeId { get; set; }
}
