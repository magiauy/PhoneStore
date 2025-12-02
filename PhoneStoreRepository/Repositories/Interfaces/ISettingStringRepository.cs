using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStoreRepository.Repositories.Interfaces
{
    public interface ISettingStringRepository : IRepository<SettingString>
    {
        /// <summary>
        /// Get a setting by its unique code
        /// </summary>
        SettingString? GetByCode(string code);

        /// <summary>
        /// Get all settings of a specific type
        /// </summary>
        IEnumerable<SettingString> GetByType(string type);

        /// <summary>
        /// Get settings filtered by code or type with pagination
        /// </summary>
        IEnumerable<SettingString> GetSettingsFiltered(string? searchText, int page, int pageSize);

        /// <summary>
        /// Get total record count for filtered settings
        /// </summary>
        int GetTotalRecords(string? searchText);

        /// <summary>
        /// Get total pages for filtered settings
        /// </summary>
        int GetTotalPages(string? searchText, int pageSize);

        /// <summary>
        /// Check if a setting code already exists
        /// </summary>
        bool CodeExists(string code, int? excludeId = null);
    }
}
