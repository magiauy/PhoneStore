using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminBrandService : IAdminBrandService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminBrandService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<AdminBrandDto>> GetBrandsAsync(int page, int pageSize, string? searchTerm = null)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        page = page < 1 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var query = context.Brands.AsNoTracking();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var normalizedSearch = searchTerm.Trim().ToLower();
            query = query.Where(b => b.Name.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync();

        if (totalCount == 0)
        {
            return new PagedResult<AdminBrandDto>(new List<AdminBrandDto>(), 0, page, pageSize);
        }

        // Get brands with product count
        var items = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new AdminBrandDto
            {
                Id = b.Id,
                Name = b.Name,
                ProductCount = context.Products.Count(p => p.BrandId == b.Id)
            })
            .ToListAsync();

        return new PagedResult<AdminBrandDto>(items, totalCount, page, pageSize);
    }

    public async Task<AdminBrandDto?> GetBrandByIdAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        return await context.Brands
            .AsNoTracking()
            .Where(b => b.Id == id)
            .Select(b => new AdminBrandDto
            {
                Id = b.Id,
                Name = b.Name,
                ProductCount = context.Products.Count(p => p.BrandId == b.Id)
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> CreateBrandAsync(CreateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return false;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Check if brand name already exists
        var exists = await context.Brands
            .AnyAsync(b => b.Name.ToLower() == dto.Name.Trim().ToLower());

        if (exists)
            return false;

        var brand = new BrandEntity
        {
            Name = dto.Name.Trim()
        };

        context.Brands.Add(brand);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateBrandAsync(int id, UpdateBrandDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return false;

        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var brand = await context.Brands.FindAsync(id);
        if (brand == null)
            return false;

        // Check if new name already exists (excluding current brand)
        var exists = await context.Brands
            .AnyAsync(b => b.Id != id && b.Name.ToLower() == dto.Name.Trim().ToLower());

        if (exists)
            return false;

        brand.Name = dto.Name.Trim();
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteBrandAsync(int id)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        // Check if brand has products
        var hasProducts = await context.Products.AnyAsync(p => p.BrandId == id);
        if (hasProducts)
            return false;

        var brand = await context.Brands.FindAsync(id);
        if (brand == null)
            return false;

        context.Brands.Remove(brand);
        await context.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetProductCountByBrandAsync(int brandId)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Products.CountAsync(p => p.BrandId == brandId);
    }
}
