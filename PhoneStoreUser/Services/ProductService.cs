using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class ProductService : IProductService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public ProductService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<ProductEntity>> GetProductsAsync(string? search, int page = 1, int pageSize = 10)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search) || p.Sku.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductEntity>(items, totalCount, page, pageSize);
    }

    public async Task<ProductEntity?> GetProductByIdAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Model)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> CreateProductAsync(ProductEntity product)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        product.CreatedAt = DateTime.UtcNow;
        if (product.Model != null)
        {
            product.Model.CreatedAt = DateTime.UtcNow;
        }
        context.Products.Add(product);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateProductAsync(ProductEntity product)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.Products
            .Include(p => p.Model)
            .FirstOrDefaultAsync(p => p.Id == product.Id);

        if (existing == null) return false;

        // Update properties
        existing.Name = product.Name;
        existing.Sku = product.Sku;
        existing.Price = product.Price;
        existing.Cost = product.Cost;
        existing.CategoryId = product.CategoryId;
        existing.BrandId = product.BrandId;
        existing.ModelId = product.ModelId;
        existing.IsSerialTracked = product.IsSerialTracked;
        existing.WarrantyMonths = product.WarrantyMonths;
        existing.Status = product.Status;

        // Update Model if present
        if (product.Model != null)
        {
            if (existing.Model == null)
            {
                existing.Model = product.Model;
                existing.Model.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                existing.Model.DefaultImageUrl = product.Model.DefaultImageUrl;
                existing.Model.UpdatedAt = DateTime.UtcNow;
                // Update other model properties if needed
            }
        }

        context.Products.Update(existing);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var product = await context.Products.FindAsync(id);
        if (product == null) return false;

        context.Products.Remove(product);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<List<ProductModelEntity>> GetProductModelsAsync()
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductModels.OrderBy(m => m.Name).ToListAsync();
    }

    public async Task<List<ProductAttributeEntity>> GetAttributesByModelIdAsync(int modelId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        // Get attribute IDs linked to this model
        var attributeIds = await context.ProductModelAttributes
            .Where(pma => pma.ModelId == modelId)
            .Select(pma => pma.AttributeId)
            .ToListAsync();

        // Fetch the actual attributes
        return await context.ProductAttributes
            .Where(pa => attributeIds.Contains(pa.Id))
            .ToListAsync();
    }

    public async Task<List<ProductAttributeOptionEntity>> GetAttributeOptionsAsync(int attributeId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributeOptions
            .Where(o => o.AttributeId == attributeId && (o.IsActive == null || o.IsActive == 1))
            .OrderBy(o => o.SortOrder)
            .ToListAsync();
    }

    public async Task<List<ProductAttributeValueEntity>> GetProductAttributeValuesAsync(int productId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributeValues
            .Where(pav => pav.ProductId == productId)
            .ToListAsync();
    }

    public async Task SaveProductAttributeValuesAsync(int productId, List<ProductAttributeValueEntity> values)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        // Remove existing values for this product
        var existingValues = await context.ProductAttributeValues
            .Where(pav => pav.ProductId == productId)
            .ToListAsync();

        context.ProductAttributeValues.RemoveRange(existingValues);

        // Add new values
        foreach (var val in values)
        {
            val.ProductId = productId; // Ensure ProductId is set
            val.Id = 0; // Ensure it's treated as new
            context.ProductAttributeValues.Add(val);
        }

        await context.SaveChangesAsync();
    }
}
