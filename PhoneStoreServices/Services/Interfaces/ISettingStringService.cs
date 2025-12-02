using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStore.Services.ViewModels;
using System.Collections.Generic;

namespace PhoneStore.Services.Interfaces
{
    public interface ISettingStringService
    {
        /// <summary>
        /// Get a setting by ID
        /// </summary>
        SettingString? GetById(int id);

        /// <summary>
        /// Get a setting by its unique code
        /// </summary>
        SettingString? GetByCode(string code);

        /// <summary>
        /// Get a setting by enum code
        /// </summary>
        SettingString? GetByCode(SystemSettingCode code);

        /// <summary>
        /// Get all settings
        /// </summary>
        IEnumerable<SettingString> GetAll();

        /// <summary>
        /// Get settings by type
        /// </summary>
        IEnumerable<SettingString> GetByType(string type);

        /// <summary>
        /// Get settings filtered with pagination
        /// </summary>
        SettingStringResult GetSettingsFiltered(string? searchText, int page = 1, int pageSize = 10);

        /// <summary>
        /// Update an existing setting (only value can be changed)
        /// </summary>
        bool Update(SettingString setting);

        /// <summary>
        /// Get a setting value by code, with optional default value
        /// </summary>
        string GetValue(string code, string defaultValue = "");

        /// <summary>
        /// Get a setting value by enum code, with optional default value
        /// </summary>
        string GetValue(SystemSettingCode code, string defaultValue = "");

        /// <summary>
        /// Update a setting value by code (only updates existing settings)
        /// </summary>
        bool SetValue(string code, string value);

        /// <summary>
        /// Update a setting value by enum code
        /// </summary>
        bool SetValue(SystemSettingCode code, string value);

        /// <summary>
        /// Initialize all settings from SystemSettingCode enum.
        /// Creates missing settings with default values.
        /// </summary>
        void InitializeSettings();

        /// <summary>
        /// Check if all settings from enum exist in database
        /// </summary>
        bool AreAllSettingsInitialized();
    }
}
