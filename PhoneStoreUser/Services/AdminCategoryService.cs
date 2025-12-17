using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminCategoryService : IAdminCategoryService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminCategoryService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<AdminCategoryDto>> GetCategoriesAsync(int page, int pageSize, string? searchTerm = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        page = page < 1 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.Categories.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch = searchTerm.Trim().ToLower();
            query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync();

        if (totalCount == 0)
        {
            return new PagedResult<AdminCategoryDto>(new List<AdminCategoryDto>(), 0, page, pageSize);
        }

        // Get all categories for parent name lookup
        var allCategories = await context.Categories.AsNoTracking().ToListAsync();
        var categoryDict = allCategories.ToDictionary(c => c.Id, c => c.Name);

        var items = await query
            .OrderBy(c => c.ParentId ?? 0)
            .ThenBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name ?? "",
                ParentId = c.ParentId,
                Note = c.Note,
                ProductCount = context.Products.Count(p => p.CategoryId == c.Id)
            })
            .ToListAsync();

        // Set parent names
        foreach (var item in items)
        {
            if (item.ParentId.HasValue && categoryDict.TryGetValue(item.ParentId.Value, out var parentName))
            {
                item.ParentName = parentName;
            }
        }

        return new PagedResult<AdminCategoryDto>(items, totalCount, page, pageSize);
    }

    public async Task<List<AdminCategoryDto>> GetAllCategoriesAsync()
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name ?? "",
                ParentId = c.ParentId,
                Note = c.Note
            })
            .ToListAsync();

        return categories;
    }

    public async Task<AdminCategoryDto?> GetCategoryByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var category = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new AdminCategoryDto
            {
                Id = c.Id,
                Name = c.Name ?? "",
                ParentId = c.ParentId,
                Note = c.Note,
                ProductCount = context.Products.Count(p => p.CategoryId == c.Id)
            })
            .FirstOrDefaultAsync();

        if (category != null && category.ParentId.HasValue)
        {
            var parent = await context.Categories
                .AsNoTracking()
                .Where(c => c.Id == category.ParentId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
            category.ParentName = parent;
        }

        return category;
    }

    public async Task<bool> CreateCategoryAsync(CreateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return false;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Check if category name already exists
        var exists = await context.Categories
            .AnyAsync(c => c.Name != null && c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (exists)
            return false;

        var category = new CategoryEntity
        {
            Name = dto.Name.Trim(),
            ParentId = dto.ParentId,
            Note = dto.Note?.Trim()
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return false;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var category = await context.Categories.FindAsync(id);
        if (category == null)
            return false;

        // Prevent setting self as parent
        if (dto.ParentId == id)
            return false;

        // Check if new name already exists (excluding current category)
        var exists = await context.Categories
            .AnyAsync(c => c.Id != id && c.Name != null && c.Name.ToLower() == dto.Name.Trim().ToLower());

        if (exists)
            return false;

        category.Name = dto.Name.Trim();
        category.ParentId = dto.ParentId;
        category.Note = dto.Note?.Trim();

        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Check if category has products
        var hasProducts = await context.Products.AnyAsync(p => p.CategoryId == id);
        if (hasProducts)
            return false;

        // Check if category has children
        var hasChildren = await context.Categories.AnyAsync(c => c.ParentId == id);
        if (hasChildren)
            return false;

        var category = await context.Categories.FindAsync(id);
        if (category == null)
            return false;

        context.Categories.Remove(category);
        await context.SaveChangesAsync();

        return true;
    }
}
