using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class ReviewService(IDbContextFactory<AppDbContext> dbContextFactory) : IReviewService
{
    public async Task<List<ReviewEntity>> GetReviewsByProductIdAsync(int productId)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Reviews
            .Include(r => r.Person)
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddReviewAsync(ReviewEntity review)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        
        // 1. Check if user is authenticated (PersonId > 0)
        if (review.PersonId <= 0)
        {
            throw new InvalidOperationException("Bạn cần đăng nhập để đánh giá sản phẩm.");
        }

        // 2. Count valid invoices containing the product
        // Status: "paid", "completed", "delivering"
        var validInvoiceCount = await context.Invoices
            .Where(i => i.PersonId == review.PersonId && 
                        (i.Status == "paid" || i.Status == "completed" || i.Status == "delivering") &&
                        i.Lines.Any(l => l.ProductId == review.ProductId))
            .CountAsync();

        if (validInvoiceCount == 0)
        {
            throw new InvalidOperationException("Bạn chưa mua sản phẩm này hoặc đơn hàng chưa hoàn tất.");
        }

        // 3. Count existing reviews by this user for this product
        var existingReviewCount = await context.Reviews
            .CountAsync(r => r.PersonId == review.PersonId && r.ProductId == review.ProductId);

        // 4. Rule: ExistingReviews < ValidInvoices
        if (existingReviewCount >= validInvoiceCount)
        {
            throw new InvalidOperationException($"Bạn đã đánh giá sản phẩm này {existingReviewCount} lần. Số lần đánh giá tối đa tương ứng với số đơn hàng đã mua ({validInvoiceCount}).");
        }

        context.Reviews.Add(review);
        await context.SaveChangesAsync();
    }

    public async Task<double> GetAverageRatingAsync(int productId)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        var ratings = await context.Reviews
            .Where(r => r.ProductId == productId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (ratings.Count == 0) return 0;

        return ratings.Average();
    }

    public async Task<(double AverageRating, int TotalReviews)> GetReviewSummaryAsync(int productModelId)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        var ratings = await context.Reviews
            .Include(r => r.Product)
            .Where(r => r.Product.ModelId == productModelId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (ratings.Count == 0) return (0, 0);

        return (ratings.Average(), ratings.Count);
    }

    public async Task<List<ReviewEntity>> GetReviewsByProductModelIdAsync(int productModelId)
    {
        using var context = await dbContextFactory.CreateDbContextAsync();
        return await context.Reviews
            .Include(r => r.Person)
            .Include(r => r.Product)
            .Where(r => r.Product.ModelId == productModelId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
