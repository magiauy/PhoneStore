using PhoneStoreRepository.Models;
using PhoneStore.Services.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStore.Services.Interfaces
{
    public interface IPromotionCodeService
    {
        PromotionCode? GetPromotionCodeById(int promotionCodeId);
        bool Insert(PromotionCode promotionCode);
        bool Update(PromotionCode promotionCode);
        IEnumerable<PromotionCode> GetAll();
        
        PromotionCodeResult GetPromotionCodesFiltered(
            string? code,
            int? promotionId,
            bool? isActive,
            int page = 1,
            int pageSize = 10);

        PromotionCodeResult GetPromotionCodesFilteredWithPromotionNames(
            string? code,
            int? promotionId,
            bool? isActive,
            int page = 1,
            int pageSize = 10);

        PromotionCodeResult GetPromotionCodesWithAdvancedFilter(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit,
            int page = 1,
            int pageSize = 10);
            
        void ActivatePromotionCode(int promotionCodeId);
        void DeactivatePromotionCode(int promotionCodeId);
    }
}

