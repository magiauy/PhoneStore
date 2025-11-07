using PhoneStoreAdmin.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PhoneStoreAdmin.Repositories.Interfaces
{
    public interface IPromotionCodeRepository : IRepository<PromotionCode>
    {
        /// <summary>
        /// Get promotion code by code (synchronous)
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>PromotionCode if found, null otherwise</returns>
        PromotionCode? GetByCode(string code);

        /// <summary>
        /// Check if a promotion code is valid (active, not expired, usage not exceeded)
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>True if valid, false otherwise</returns>
        bool IsValid(string code);

        /// <summary>
        /// Increment the used count of a promotion code
        /// </summary>
        /// <param name="id">Promotion code ID</param>
        /// <returns>True if successful, false otherwise</returns>
        bool IncrementUsedCount(int id);

        /// <summary>
        /// Get promotion code by ID async
        /// </summary>
        /// <param name="id">Promotion code ID</param>
        /// <returns>PromotionCode if found, null otherwise</returns>
        Task<PromotionCode?> GetByIdAsync(int id);

        /// <summary>
        /// Get promotion code by code async
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>PromotionCode if found, null otherwise</returns>
        Task<PromotionCode?> GetByCodeAsync(string code);

        /// <summary>
        /// Get all promotion codes async
        /// </summary>
        /// <returns>List of all promotion codes</returns>
        Task<IEnumerable<PromotionCode>> GetAllAsync();

        /// <summary>
        /// Add new promotion code async
        /// </summary>
        /// <param name="entity">PromotionCode to add</param>
        /// <returns>Added promotion code if successful, null otherwise</returns>
        Task<PromotionCode?> AddAsync(PromotionCode entity);

        /// <summary>
        /// Update existing promotion code async
        /// </summary>
        /// <param name="entity">PromotionCode to update</param>
        Task UpdateAsync(PromotionCode entity);

        /// <summary>
        /// Delete promotion code async
        /// </summary>
        /// <param name="id">Promotion code ID to delete</param>
        Task DeleteAsync(int id);

        /// <summary>
        /// Check if a promotion code is valid async
        /// </summary>
        /// <param name="code">Promotion code string</param>
        /// <returns>True if valid, false otherwise</returns>
        Task<bool> IsValidAsync(string code);

        /// <summary>
        /// Increment the used count of a promotion code async
        /// </summary>
        /// <param name="id">Promotion code ID</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> IncrementUsedCountAsync(int id);
    }
}
