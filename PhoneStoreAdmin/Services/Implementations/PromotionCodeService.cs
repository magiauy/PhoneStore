using Microsoft.Extensions.Logging;
using PhoneStoreAdmin.Models;
using PhoneStoreAdmin.Repositories.Interfaces;
using PhoneStoreAdmin.Services.Interfaces;
using PhoneStoreAdmin.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Implementations
{
    public class PromotionCodeService : IPromotionCodeService
    {
        private readonly IPromotionCodeRepository _promotionCodeRepository;
        private readonly ILogger<PromotionCodeService> _logger;
        public PromotionCodeService(IPromotionCodeRepository promotionCodeRepository)
        {
            _promotionCodeRepository = promotionCodeRepository ?? throw new ArgumentNullException(nameof(promotionCodeRepository));
        }

        public async Task<PromotionCode?> GetPromotionCodeByIdAsync(int promotionCodeId)
        {
            try
            {
                Logger.Info($"Getting promotion code by ID: {promotionCodeId}");
                return await _promotionCodeRepository.GetByIdAsync(promotionCodeId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get promotion code by ID: {promotionCodeId}", ex);
                return null;
            }
        }

        public async Task<PromotionCode?> GetPromotionCodeByCodeAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return null;

                Logger.Info($"Getting promotion code by code: {code}");
                return await _promotionCodeRepository.GetByCodeAsync(code);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get promotion code by code: {code}", ex);
                return null;
            }
        }

        public async Task<IEnumerable<PromotionCode>> GetAllPromotionCodesAsync()
        {
            try
            {
                Logger.Info("Getting all active promotion codes");
                return await _promotionCodeRepository.GetAllAsync();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all promotion codes", ex);
                return new List<PromotionCode>();
            }
        }

        public async Task<PromotionCode?> CreatePromotionCodeAsync(PromotionCode promotionCode)
        {
            try
            {
                if (promotionCode == null)
                    return null;

                Logger.Info($"Creating promotion code: {promotionCode.Code}");
                return await _promotionCodeRepository.AddAsync(promotionCode);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to create promotion code: {promotionCode?.Code}", ex);
                return null;
            }
        }

        public async Task<bool> UpdatePromotionCodeAsync(PromotionCode promotionCode)
        {
            try
            {
                if (promotionCode == null)
                    return false;

                Logger.Info($"Updating promotion code: {promotionCode.Code}");
                await _promotionCodeRepository.UpdateAsync(promotionCode);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update promotion code: {promotionCode?.Code}", ex);
                return false;
            }
        }

        public async Task<bool> DeletePromotionCodeAsync(int promotionCodeId)
        {
            try
            {
                Logger.Info($"Deleting promotion code ID: {promotionCodeId}");
                await _promotionCodeRepository.DeleteAsync(promotionCodeId);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to delete promotion code ID: {promotionCodeId}", ex);
                return false;
            }
        }

        public async Task<bool> IsPromotionCodeValidAsync(string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                Logger.Info($"Checking if promotion code is valid: {code}");
                return await _promotionCodeRepository.IsValidAsync(code);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to validate promotion code: {code}", ex);
                return false;
            }
        }

        public async Task<bool> IncrementUsedCountAsync(int promotionCodeId)
        {
            try
            {
                Logger.Info($"Incrementing used count for promotion code ID: {promotionCodeId}");
                return await _promotionCodeRepository.IncrementUsedCountAsync(promotionCodeId);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to increment used count for promotion code ID: {promotionCodeId}", ex);
                return false;
            }
        }
    }
}

