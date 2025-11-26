using PhoneStoreUser.Data;

namespace PhoneStoreUser.Services;

public interface IPromotionService
{
    Task<(bool isValid, string message, PromotionCodeEntity? code)> ValidateCouponAsync(string code, decimal orderTotal);
    Task IncrementUsageCountAsync(int promotionCodeId);
}
