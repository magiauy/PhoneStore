using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminPromotionService : IAdminPromotionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminPromotionService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<PromotionEntity>> GetPromotionsAsync(string? search, int page = 1, int pageSize = 10)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Promotions.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.StartDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<PromotionEntity>(items, totalCount, page, pageSize);
    }

    public async Task<PromotionEntity?> GetPromotionByIdAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.Promotions.FindAsync(id);
    }

    public async Task<bool> CreatePromotionAsync(PromotionEntity promotion)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        context.Promotions.Add(promotion);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdatePromotionAsync(PromotionEntity promotion)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.Promotions.FindAsync(promotion.Id);
        if (existing == null) return false;

        existing.Name = promotion.Name;
        existing.Description = promotion.Description;
        existing.StartDate = promotion.StartDate;
        existing.EndDate = promotion.EndDate;
        existing.IsActive = promotion.IsActive;

        context.Promotions.Update(existing);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeletePromotionAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var promotion = await context.Promotions.FindAsync(id);
        if (promotion == null) return false;

        // Optionally check for dependencies or just soft delete (IsActive = false)
        // For now, let's just deactivate it to preserve history
        promotion.IsActive = false;
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<List<PromotionCodeEntity>> GetCodesByPromotionIdAsync(int promotionId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        return await context.PromotionCodes
            .Where(pc => pc.PromotionId == promotionId)
            .OrderBy(pc => pc.Code)
            .ToListAsync();
    }

    public async Task<bool> CreatePromotionCodeAsync(PromotionCodeEntity code)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        // Check for duplicate code
        if (await context.PromotionCodes.AnyAsync(pc => pc.Code == code.Code))
        {
            return false;
        }

        context.PromotionCodes.Add(code);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdatePromotionCodeAsync(PromotionCodeEntity code)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var existing = await context.PromotionCodes.FindAsync(code.Id);
        if (existing == null) return false;

        existing.Code = code.Code;
        existing.DiscountAmount = code.DiscountAmount;
        existing.MinimumAmount = code.MinimumAmount;
        existing.UsageLimit = code.UsageLimit;
        existing.IsActive = code.IsActive;

        context.PromotionCodes.Update(existing);
        return await context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeletePromotionCodeAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var code = await context.PromotionCodes.FindAsync(id);
        if (code == null) return false;

        code.IsActive = false;
        return await context.SaveChangesAsync() > 0;
    }
}
