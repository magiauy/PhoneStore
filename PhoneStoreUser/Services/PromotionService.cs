using Microsoft.EntityFrameworkCore;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public class PromotionService : IPromotionService
{
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public PromotionService(IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<(bool isValid, string message, PromotionCodeEntity? code)> ValidateCouponAsync(string code, decimal orderTotal)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();

        var promotionCode = await context.PromotionCodes
            .Include(pc => pc.Promotion)
            .FirstOrDefaultAsync(pc => pc.Code == code);

        if (promotionCode == null)
        {
            return (false, "Mã khuyến mãi không tồn tại.", null);
        }

        if (promotionCode.IsActive == false)
        {
            return (false, "Mã khuyến mãi đã hết hạn hoặc bị vô hiệu hóa.", null);
        }

        if (promotionCode.Promotion == null || promotionCode.Promotion.IsActive == false)
        {
            return (false, "Chương trình khuyến mãi đã kết thúc.", null);
        }

        var now = DateTime.UtcNow; // Or local time if needed, but UTC is safer
        // Adjust for timezone if necessary, assuming server time matches DB expectation or using UTC consistently
        // Given the context, let's assume DB stores local time or we compare appropriately. 
        // Ideally DB dates should be UTC. Let's stick to DateTime.Now if the app uses local time, or UtcNow.
        // Looking at other files, CreatedAt is DateTime.UtcNow.
        
        if (now < promotionCode.Promotion.StartDate || now > promotionCode.Promotion.EndDate)
        {
            return (false, "Mã khuyến mãi chưa bắt đầu hoặc đã hết hạn.", null);
        }

        if (promotionCode.UsageLimit.HasValue && promotionCode.UsedCount >= promotionCode.UsageLimit.Value)
        {
            return (false, "Mã khuyến mãi đã hết lượt sử dụng.", null);
        }

        if (orderTotal < promotionCode.MinimumAmount)
        {
            return (false, $"Đơn hàng chưa đạt giá trị tối thiểu {promotionCode.MinimumAmount:N0}đ để áp dụng mã này.", null);
        }

        return (true, "Áp dụng mã khuyến mãi thành công.", promotionCode);
    }

    public async Task IncrementUsageCountAsync(int promotionCodeId)
    {
        using var context = await _dbContextFactory.CreateDbContextAsync();
        var code = await context.PromotionCodes.FindAsync(promotionCodeId);
        if (code != null)
        {
            code.UsedCount++;
            await context.SaveChangesAsync();
        }
    }
}
