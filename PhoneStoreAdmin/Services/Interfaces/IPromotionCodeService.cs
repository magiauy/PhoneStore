using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Services.Interfaces
{
    public interface IPromotionCodeService
    {
        /// <summary>
        /// Get promotion code by ID
        /// </summary>
        /// <param name="promotionCodeId">Promotion Code ID</param>
        /// <returns>PromotionCode if found, null otherwise</returns>
        Task<PromotionCode?> GetPromotionCodeByIdAsync(int promotionCodeId);

        /// <summary>
        /// Get promotion code by code string
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>PromotionCode if found, null otherwise</returns>
        Task<PromotionCode?> GetPromotionCodeByCodeAsync(string code);

        /// <summary>
        /// Get all available promotion codes
        /// </summary>
        /// <returns>List of all active PromotionCodes</returns>
        Task<IEnumerable<PromotionCode>> GetAllPromotionCodesAsync();

        /// <summary>
        /// Create a new promotion code
        /// </summary>
        /// <param name="promotionCode">Promotion code to create</param>
        /// <returns>Created promotion code if successful, null otherwise</returns>
        Task<PromotionCode?> CreatePromotionCodeAsync(PromotionCode promotionCode);

        /// <summary>
        /// Update promotion code information
        /// </summary>
        /// <param name="promotionCode">Promotion code to update</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> UpdatePromotionCodeAsync(PromotionCode promotionCode);

        /// <summary>
        /// Delete promotion code by ID
        /// </summary>
        /// <param name="promotionCodeId">Promotion code ID</param>
        /// <returns>True if deleted successfully, false otherwise</returns>
        Task<bool> DeletePromotionCodeAsync(int promotionCodeId);

        /// <summary>
        /// Check if a promotion code is valid and active
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> IsPromotionCodeValidAsync(string code);

        /// <summary>
        /// Increment the used count for a promotion code
        /// </summary>
        /// <param name="promotionCodeId">Promotion code ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> IncrementUsedCountAsync(int promotionCodeId);
    }
}

