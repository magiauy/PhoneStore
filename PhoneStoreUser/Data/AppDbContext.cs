using Microsoft.EntityFrameworkCore;

namespace PhoneStoreUser.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<BrandEntity> Brands => Set<BrandEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<PersonEntity> People => Set<PersonEntity>();
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<InvoiceEntity> Invoices => Set<InvoiceEntity>();
    public DbSet<InvoiceLineEntity> InvoiceLines => Set<InvoiceLineEntity>();

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

public class InvoiceEntity
{
    public int Id { get; set; }
    public int? PersonId { get; set; }
    public int? PromotionCodeId { get; set; }
    public int CreatedBy { get; set; }
    public DateTime InvoiceDate { get; set; }
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
}
