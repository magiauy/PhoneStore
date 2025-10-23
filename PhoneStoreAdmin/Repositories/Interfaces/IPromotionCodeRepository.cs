using PhoneStoreAdmin.Models;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPromotionCodeRepository : IRepository<PromotionCode>
    {
        IEnumerable<PromotionCode> GetPromotionCodesFiltered(
            string? code,
            int? promotionId,
            bool? isActive,
            int page,
            int pageSize);

        int GetTotalRecords(
            string? code,
            int? promotionId,
            bool? isActive);

        int GetTotalPages(
            string? code,
            int? promotionId,
            bool? isActive,
            int pageSize);

        IEnumerable<PromotionCode> GetPromotionCodesWithAdvancedFilter(
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
            int page,
            int pageSize);

        int GetAdvancedFilterTotalRecords(
            string? code,
            int? promotionId,
            string? promotionName,
            bool? isActive,
            decimal? minDiscountAmount,
            decimal? maxDiscountAmount,
            decimal? minMinimumAmount,
            decimal? maxMinimumAmount,
            int? minUsageLimit,
            int? maxUsageLimit);

        int GetAdvancedFilterTotalPages(
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
            int pageSize);
    }
}