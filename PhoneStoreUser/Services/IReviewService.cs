using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IReviewService
{
    Task<List<ReviewEntity>> GetReviewsByProductIdAsync(int productId);
    Task AddReviewAsync(ReviewEntity review);
    Task<double> GetAverageRatingAsync(int productId);
    Task<(double AverageRating, int TotalReviews)> GetReviewSummaryAsync(int productModelId);
    Task<List<ReviewEntity>> GetReviewsByProductModelIdAsync(int productModelId);
}
