using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class AdminReviewService : IAdminReviewService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AdminReviewService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<PagedResult<ReviewEntity>> GetReviewsAsync(string? search, int page = 1, int pageSize = 10)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var query = context.Reviews
            .Include(r => r.Product)
            .Include(r => r.Person)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => 
                (r.Product != null && r.Product.Name.Contains(search)) || 
                (r.Person != null && r.Person.FullName.Contains(search)) ||
                (r.Comment != null && r.Comment.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<ReviewEntity>(items, totalCount, page, pageSize);
    }

    public async Task<bool> DeleteReviewAsync(int id)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var review = await context.Reviews.FindAsync(id);
        if (review == null) return false;

        context.Reviews.Remove(review);
        return await context.SaveChangesAsync() > 0;
    }
}
