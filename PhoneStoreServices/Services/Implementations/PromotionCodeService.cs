using PhoneStoreRepository.Models;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.Implementations
{
    public class PromotionCodeService : IPromotionCodeService
    {
        private readonly IPromotionCodeRepository _promotionCodeRepository;
        private readonly IPromotionRepository _promotionRepository;

        public PromotionCodeService(IPromotionCodeRepository promotionCodeRepository, IPromotionRepository promotionRepository)
        {
            _promotionCodeRepository = promotionCodeRepository ?? throw new ArgumentNullException(nameof(promotionCodeRepository));
            _promotionRepository = promotionRepository ?? throw new ArgumentNullException(nameof(promotionRepository));
        }

        public IEnumerable<PromotionCode> GetAll()
        {
            try
            {
                return _promotionCodeRepository.GetAll();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get PromotionCode all", ex);
                return Enumerable.Empty<PromotionCode>();
            }
        }

        public bool Insert(PromotionCode promotionCode)
        {
            try
            {
                _promotionCodeRepository.Insert(promotionCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to insert PromotionCode", ex);
                return false;
            }
        }

        public bool Update(PromotionCode promotionCode)
        {
            try
            {
                _promotionCodeRepository.Update(promotionCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update PromotionCode {promotionCode.Id}", ex);
                return false;
            }
        }

        public PromotionCode? GetPromotionCodeById(int promotionCodeId)
        {
            try
            {
                Logger.Info($"Getting PromotionCode by ID: {promotionCodeId}");
                return _promotionCodeRepository.GetById(promotionCodeId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get PromotionCode by ID: {promotionCodeId}", ex);
                return null;
            }
        }

        public PromotionCodeResult GetPromotionCodesFiltered(
            string? code,
            int? promotionId,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var promotionCodes = _promotionCodeRepository.GetPromotionCodesFiltered(code, promotionId, isActive, page, pageSize);
                var totalPages = _promotionCodeRepository.GetTotalPages(code, promotionId, isActive, pageSize);
                var totalRecords = _promotionCodeRepository.GetTotalRecords(code, promotionId, isActive);
                return new PromotionCodeResult(promotionCodes, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered promotion codes", ex);
                return new PromotionCodeResult(new PromotionCode[0], new InfoTable(0, 0));
            }
        }

        public PromotionCodeResult GetPromotionCodesFilteredWithPromotionNames(
            string? code,
            int? promotionId,
            bool? isActive,
            int page = 1,
            int pageSize = 10)
        {
            try
            {
                var promotionCodes = _promotionCodeRepository.GetPromotionCodesFiltered(code, promotionId, isActive, page, pageSize);
                var totalPages = _promotionCodeRepository.GetTotalPages(code, promotionId, isActive, pageSize);
                var totalRecords = _promotionCodeRepository.GetTotalRecords(code, promotionId, isActive);
                
                // Create result with promotion names populated
                var promotionCodeViewModels = promotionCodes.Select(pc => 
                {
                    var viewModel = new PromotionCodeViewModel(pc);
                    try
                    {
                        var promotion = _promotionRepository.GetById(pc.PromotionId);
                        viewModel.PromotionName = promotion?.Name ?? "Unknown";
                    }
                    catch
                    {
                        viewModel.PromotionName = "Unknown";
                    }
                    return viewModel;
                }).ToList();

                return new PromotionCodeResult
                {
                    PromotionCodes = promotionCodeViewModels,
                    Info = new InfoTable(totalRecords, totalPages)
                };

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered promotion codes with names", ex);
                return new PromotionCodeResult(new PromotionCode[0], new InfoTable(0, 0));
            }
        }

        public PromotionCodeResult GetPromotionCodesWithAdvancedFilter(
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
            int pageSize = 10)
        {
            try
            {
                var promotionCodes = _promotionCodeRepository.GetPromotionCodesWithAdvancedFilter(
                    code, promotionId, promotionName, isActive,
                    minDiscountAmount, maxDiscountAmount,
                    minMinimumAmount, maxMinimumAmount,
                    minUsageLimit, maxUsageLimit,
                    page, pageSize);
                    
                var totalPages = _promotionCodeRepository.GetAdvancedFilterTotalPages(
                    code, promotionId, promotionName, isActive,
                    minDiscountAmount, maxDiscountAmount,
                    minMinimumAmount, maxMinimumAmount,
                    minUsageLimit, maxUsageLimit,
                    pageSize);
                    
                var totalRecords = _promotionCodeRepository.GetAdvancedFilterTotalRecords(
                    code, promotionId, promotionName, isActive,
                    minDiscountAmount, maxDiscountAmount,
                    minMinimumAmount, maxMinimumAmount,
                    minUsageLimit, maxUsageLimit);
                
                // Create result with promotion names populated
                var promotionCodeViewModels = promotionCodes.Select(pc => 
                {
                    var viewModel = new PromotionCodeViewModel(pc);
                    try
                    {
                        var promotion = _promotionRepository.GetById(pc.PromotionId);
                        viewModel.PromotionName = promotion?.Name ?? "Unknown";
                    }
                    catch
                    {
                        viewModel.PromotionName = "Unknown";
                    }
                    return viewModel;
                }).ToList();

                return new PromotionCodeResult
                {
                    PromotionCodes = promotionCodeViewModels,
                    Info = new InfoTable(totalRecords, totalPages)
                };

            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load promotion codes with advanced filter", ex);
                return new PromotionCodeResult(new PromotionCode[0], new InfoTable(0, 0));
            }
        }

        public void ActivatePromotionCode(int promotionCodeId)
        {
            var promotionCode = GetPromotionCodeById(promotionCodeId);
            if (promotionCode != null && !promotionCode.IsActive)
            {
                promotionCode.IsActive = true;
                Update(promotionCode);
            }
        }

        public void DeactivatePromotionCode(int promotionCodeId)
        {
            var promotionCode = GetPromotionCodeById(promotionCodeId);
            if (promotionCode != null && promotionCode.IsActive)
            {
                promotionCode.IsActive = false;
                Update(promotionCode);
            }
        }
    }
}
