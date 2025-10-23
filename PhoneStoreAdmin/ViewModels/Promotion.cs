using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.ViewModels
{
    public class PromotionViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        
        public string LocalizedStatusText
        {
            get
            {
                var key = IsActive ? "Status_Active" : "Status_Inactive";
                return PhoneStoreAdmin.Helpers.LocalizationHelper.GetString(key);
            }
        }
        
        public string StatusColor => IsActive ? "#28a745" : "#dc3545";
        public string StatusIcon => IsActive ? "\uE8BB" : "\uE711";

        public PromotionViewModel() { }

        public PromotionViewModel(Promotion promotion)
        {
            Id = promotion.Id;
            Name = promotion.Name ?? string.Empty;
            Description = promotion.Description;
            StartDate = promotion.StartDate;
            EndDate = promotion.EndDate;
            IsActive = promotion.IsActive;
        }
    }

    public class PromotionResult
    {
        public IEnumerable<PromotionViewModel> Promotions { get; set; }
        public InfoTable Info { get; set; }

        public PromotionResult(IEnumerable<Promotion> promotions, InfoTable info)
        {
            Promotions = promotions.Select(p => new PromotionViewModel(p)).ToList();
            Info = info;
        }
    }
}