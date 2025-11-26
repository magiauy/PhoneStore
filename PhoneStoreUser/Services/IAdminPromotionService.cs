using PhoneStoreUser.Components.ViewModels;
using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IAdminPromotionService
{
    Task<PagedResult<PromotionEntity>> GetPromotionsAsync(string? search, int page = 1, int pageSize = 10);
    Task<PromotionEntity?> GetPromotionByIdAsync(int id);
    Task<bool> CreatePromotionAsync(PromotionEntity promotion);
    Task<bool> UpdatePromotionAsync(PromotionEntity promotion);
    Task<bool> DeletePromotionAsync(int id);
    
    Task<List<PromotionCodeEntity>> GetCodesByPromotionIdAsync(int promotionId);
    Task<bool> CreatePromotionCodeAsync(PromotionCodeEntity code);
    Task<bool> UpdatePromotionCodeAsync(PromotionCodeEntity code);
    Task<bool> DeletePromotionCodeAsync(int id);
}
