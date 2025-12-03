using PhoneStoreRepository.Models;
using PhoneStoreRepository.Models.Enums;
using PhoneStoreRepository.Repositories.Interfaces;
using PhoneStore.Services.Interfaces;
using PhoneStoreRepository.Utils;
using PhoneStore.Services.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PhoneStore.Services.Implementations
{
    public class SettingStringService : ISettingStringService
    {
        private readonly ISettingStringRepository _settingStringRepository;

        /// <summary>
        /// Default values for each setting code
        /// </summary>
        private static readonly Dictionary<SystemSettingCode, (string Value, string Type)> DefaultSettings = new()
        {
            { SystemSettingCode.PROFIT_MARGIN, ("0.20", "number") },
        };

        public SettingStringService(ISettingStringRepository settingStringRepository)
        {
            _settingStringRepository = settingStringRepository ?? throw new ArgumentNullException(nameof(settingStringRepository));
        }

        public SettingString? GetById(int id)
        {
            try
            {
                return _settingStringRepository.GetById(id);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get setting by ID: {id}", ex);
                return null;
            }
        }

        public SettingString? GetByCode(string code)
        {
            try
            {
                return _settingStringRepository.GetByCode(code);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get setting by code: {code}", ex);
                return null;
            }
        }

        public SettingString? GetByCode(SystemSettingCode code)
        {
            return GetByCode(code.ToString());
        }

        public IEnumerable<SettingString> GetAll()
        {
            try
            {
                return _settingStringRepository.GetAll();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to get all settings", ex);
                return Enumerable.Empty<SettingString>();
            }
        }

        public IEnumerable<SettingString> GetByType(string type)
        {
            try
            {
                return _settingStringRepository.GetByType(type);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get settings by type: {type}", ex);
                return Enumerable.Empty<SettingString>();
            }
        }

        public SettingStringResult GetSettingsFiltered(string? searchText, int page = 1, int pageSize = 10)
        {
            try
            {
                var settings = _settingStringRepository.GetSettingsFiltered(searchText, page, pageSize);
                var totalPages = _settingStringRepository.GetTotalPages(searchText, pageSize);
                var totalRecords = _settingStringRepository.GetTotalRecords(searchText);
                return new SettingStringResult(settings, new InfoTable(totalRecords, totalPages));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to load filtered settings", ex);
                return new SettingStringResult(Array.Empty<SettingString>(), new InfoTable(0, 0));
            }
        }

        public bool Update(SettingString setting)
        {
            try
            {
                // Only allow updating value, not code or type
                var existing = _settingStringRepository.GetById(setting.Id);
                if (existing == null)
                {
                    Logger.Warning($"Setting with ID {setting.Id} not found for update");
                    return false;
                }

                // Keep original code and type, only update value
                existing.Value = setting.Value;
                _settingStringRepository.Update(existing);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to update setting {setting.Id}", ex);
                return false;
            }
        }

        public string GetValue(string code, string defaultValue = "")
        {
            try
            {
                var setting = _settingStringRepository.GetByCode(code);
                return setting?.Value ?? defaultValue;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to get setting value for code: {code}", ex);
                return defaultValue;
            }
        }

        public string GetValue(SystemSettingCode code, string defaultValue = "")
        {
            return GetValue(code.ToString(), defaultValue);
        }

        public bool SetValue(string code, string value)
        {
            try
            {
                var existing = _settingStringRepository.GetByCode(code);
                if (existing == null)
                {
                    Logger.Warning($"Setting with code '{code}' not found. Cannot create new settings.");
                    return false;
                }

                existing.Value = value;
                _settingStringRepository.Update(existing);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to set setting value for code: {code}", ex);
                return false;
            }
        }

        public bool SetValue(SystemSettingCode code, string value)
        {
            return SetValue(code.ToString(), value);
        }

        public void InitializeSettings()
        {
            try
            {
                foreach (var settingCode in Enum.GetValues<SystemSettingCode>())
                {
                    var code = settingCode.ToString();
                    var existing = _settingStringRepository.GetByCode(code);
                    
                    if (existing == null)
                    {
                        var defaults = DefaultSettings[settingCode];
                        var newSetting = new SettingString(code, defaults.Value, defaults.Type);
                        _settingStringRepository.Insert(newSetting);
                        Logger.Info($"Initialized setting: {code} = {defaults.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize settings", ex);
            }
        }

        public bool AreAllSettingsInitialized()
        {
            try
            {
                foreach (var settingCode in Enum.GetValues<SystemSettingCode>())
                {
                    var existing = _settingStringRepository.GetByCode(settingCode.ToString());
                    if (existing == null)
                    {
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to check if all settings are initialized", ex);
                return false;
            }
        }
    }
}
