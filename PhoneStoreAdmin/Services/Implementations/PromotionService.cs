using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using PhoneStoreAdmin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class PromotionService : IPromotionService
    {
        private readonly IPromotionRepository _promotionRepository;

        public PromotionService(IPromotionRepository promotionRepository)
        {
            _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        }

        public IEnumerable<Promotion> GetAll()
        {
            try
            {
                return _promotionRepository.GetAll();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get Promotion all", ex);
                return Enumerable.Empty<Promotion>();
            }
        }

        public bool Insert(Promotion promotion)
        {
            try
            {
                _promotionRepository.Insert(promotion);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert Promotion", ex);
                return false;
            }
        }

        public bool Update(Promotion promotion)
        {
            try
            {
                _promotionRepository.Update(promotion);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update Promotion {promotion.Id}", ex);
                return false;
            }
        }

        public Promotion? GetPromotionById(int promotionId)
        {
            try
            {
                Logger.Info($"Getting Promotion by ID: {promotionId}");
                return _promotionRepository.GetById(promotionId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get Promotion by ID: {promotionId}", ex);
                return null;
            }
        }

        public PromotionResult GetPromotionsFiltered(
            string? name,
            bool? isActive,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var promotions = _promotionRepository.GetPromotionsFiltered(name, isActive, startDate, endDate, page, pageSize);
                var totalPages = _promotionRepository.GetTotalPages(name, isActive, startDate, endDate, pageSize);
                var totalRecords = _promotionRepository.GetTotalRecords(name, isActive, startDate, endDate);
                return new PromotionResult(promotions, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered promotions", ex);
                return new PromotionResult(new Promotion[0], new InfoTable(0, 0));
            }
        }

        public void ActivatePromotion(int promotionId)
        {
            var promotion = GetPromotionById(promotionId);
            if (promotion != null && !promotion.IsActive)
            {
                promotion.IsActive = true;
                Update(promotion);
            }
        }

        public void DeactivatePromotion(int promotionId)
        {
            var promotion = GetPromotionById(promotionId);
            if (promotion != null && promotion.IsActive)
            {
                promotion.IsActive = false;
                Update(promotion);
            }
        }
    }
}