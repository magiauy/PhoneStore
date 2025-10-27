using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IPromotionService
    {
        Promotion? GetPromotionById(int promotionId);
        bool Insert(Promotion promotion);
        bool Update(Promotion promotion);
        IEnumerable<Promotion> GetAll();
        
        PromotionResult GetPromotionsFiltered(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10);
            
        void ActivatePromotion(int promotionId);
        void DeactivatePromotion(int promotionId);
    }
}