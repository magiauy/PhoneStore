using PhoneStoreRepository.Models;
using System.Collections.Generic;
using System.Linq;
using PhoneStore.Services.Helpers;

namespace PhoneStore.Services.ViewModels
{
    public class PromotionCodeViewModel
    {
        public int Id { get; set; }
        public int PromotionId { get; set; }
        public string Code { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal MinimumAmount { get; set; }
        public int? UsageLimit { get; set; }
        public int UsedCount { get; set; }
        public bool IsActive { get; set; }
        public string PromotionName { get; set; } = string.Empty;
        
        public string LocalizedStatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return LocalizationHelper.GetString(key);
            }
        }
        public string UsageLimitDisplay
        {
            get
            {
                return UsageLimit?.ToString() ?? LocalizationHelper.GetString("UnlimitedPromotionCode");
            }
        }
        
        
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
        
        public string UsageStatus => UsageLimit.HasValue ? $"{UsedCount}/{UsageLimit}" : UsedCount.ToString();

        public PromotionCodeViewModel() { }

        public PromotionCodeViewModel(PromotionCode promotionCode)
        {
            Id = promotionCode.Id;
            PromotionId = promotionCode.PromotionId;
            Code = promotionCode.Code ?? string.Empty;
            DiscountAmount = promotionCode.DiscountAmount;
            MinimumAmount = promotionCode.MinimumAmount;
            UsageLimit = promotionCode.UsageLimit;
            UsedCount = promotionCode.UsedCount;
            IsActive = promotionCode.IsActive;
            PromotionName = string.Empty;
        }
    }

    public class PromotionCodeResult
    {
        public IEnumerable<PromotionCodeViewModel> PromotionCodes { get; set; }
        public InfoTable Info { get; set; }

        public PromotionCodeResult(IEnumerable<PromotionCode> promotionCodes, InfoTable info)
        {
            PromotionCodes = promotionCodes.Select(pc => new PromotionCodeViewModel(pc)).ToList();
            Info = info;
        }

        public PromotionCodeResult()
        {
            PromotionCodes = new List<PromotionCodeViewModel>();
            Info = new InfoTable(0, 0);
        }
    }
}