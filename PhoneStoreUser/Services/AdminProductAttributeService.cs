using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminProductAttributeService : IAdminProductAttributeService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminProductAttributeService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    #region Product Attributes

    public async Task<PagedResult<ProductAttributeEntity>> GetAttributesAsync(string? search, string? dataType, int page = 1, int pageSize = 10)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.ProductAttributes.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => a.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(dataType))
        {
            query = query.Where(a => a.DataType == dataType);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ProductAttributeEntity>(items, totalCount, page, pageSize);
    }

    public async Task<ProductAttributeEntity?> GetAttributeByIdAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributes.FindAsync(id);
    }

    public async Task<bool> CreateAttributeAsync(ProductAttributeEntity attribute)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        context.ProductAttributes.Add(attribute);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateAttributeAsync(ProductAttributeEntity attribute)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.ProductAttributes.FindAsync(attribute.Id);
        if (existing == null) return false;

        existing.Name = attribute.Name;
        existing.DataType = attribute.DataType;
        existing.Note = attribute.Note;

        context.ProductAttributes.Update(existing);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteAttributeAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var attribute = await context.ProductAttributes.FindAsync(id);
        if (attribute == null) return false;

        // Check if attribute has options or values
        var hasOptions = await context.ProductAttributeOptions.AnyAsync(o => o.AttributeId == id);
        var hasValues = await context.ProductAttributeValues.AnyAsync(v => v.AttributeId == id);

        if (hasOptions || hasValues)
        {
            return false; // Cannot delete attribute with existing options or values
        }

        context.ProductAttributes.Remove(attribute);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> IsAttributeNameExistsAsync(string name, int? excludeId = null)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.ProductAttributes.Where(a => a.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(a => a.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region Attribute Options

    public async Task<List<ProductAttributeOptionEntity>> GetOptionsByAttributeIdAsync(int attributeId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributeOptions
            .Where(o => o.AttributeId == attributeId)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.DisplayValue)
            .ToListAsync();
    }

    public async Task<ProductAttributeOptionEntity?> GetOptionByIdAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributeOptions.FindAsync(id);
    }

    public async Task<bool> CreateOptionAsync(ProductAttributeOptionEntity option)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        option.CreatedAt = DateTime.UtcNow;
        option.UpdatedAt = DateTime.UtcNow;
        context.ProductAttributeOptions.Add(option);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateOptionAsync(ProductAttributeOptionEntity option)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.ProductAttributeOptions.FindAsync(option.Id);
        if (existing == null) return false;

        existing.DisplayValue = option.DisplayValue;
        existing.NormalizedValue = option.NormalizedValue;
        existing.SortOrder = option.SortOrder;
        existing.IsActive = option.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;

        context.ProductAttributeOptions.Update(existing);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteOptionAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var option = await context.ProductAttributeOptions.FindAsync(id);
        if (option == null) return false;

        // Check if option is used in any attribute values
        var hasValues = await context.ProductAttributeValues.AnyAsync(v => v.OptionId == id);
        if (hasValues)
        {
            return false; // Cannot delete option with existing values
        }

        context.ProductAttributeOptions.Remove(option);
        return await context.SaveChangesAsync() > 0;
    }

    #endregion

    #region Attribute Values

    public async Task<List<ProductAttributeValueEntity>> GetValuesByProductIdAsync(int productId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.ProductAttributeValues
            .Where(v => v.ProductId == productId)
            .ToListAsync();
    }

    public async Task<bool> SaveProductAttributeValuesAsync(int productId, List<ProductAttributeValueEntity> values)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        // Remove existing values for this product
        var existingValues = await context.ProductAttributeValues
            .Where(v => v.ProductId == productId)
            .ToListAsync();

        context.ProductAttributeValues.RemoveRange(existingValues);

        // Add new values
        foreach (var value in values)
        {
            value.ProductId = productId;
            context.ProductAttributeValues.Add(value);
        }

        return await context.SaveChangesAsync() >= 0;
    }

    #endregion
}
