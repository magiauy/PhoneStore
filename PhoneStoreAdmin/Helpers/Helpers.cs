using System;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreRepository.Utils;

namespace PhoneStoreAdmin.Helpers
{
    public static class LocalizationHelper
    {
        private static readonly ResourceLoader _resourceLoader = new(); // Mặc định Resources.resw
        private static ResourceManager? _permissionResourceManager;

        /// <summary>
        /// Lấy chuỗi từ Resources.resw (file mặc định)
        /// </summary>
        public static string GetString(string key)
        {
            return GetString(key, key);
        }

        public static string GetString(string key, string? defaultValue)
        {
            try
            {
                string value = _resourceLoader.GetString(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Missing localization key: {key}, ex: {ex.Message}");
            }

            return defaultValue ?? key;
        }

        /// <summary>
        /// Lấy chuỗi từ Permission.resw
        /// </summary>
        public static string GetPermissionString(string key)
        {
            try
            {
                _permissionResourceManager ??= new ResourceManager();
                var subtree = _permissionResourceManager.MainResourceMap.TryGetSubtree("Permission");

                if (subtree is null)
                {
                    Logger.Warning("Permission resource subtree not found.");
                    return key.Replace('_', ' ');
                }

                var resource = subtree.TryGetValue(key);
                if (resource != null)
                    return resource.ValueAsString;

                Logger.Warning($"Permission key not found: {key}");
                return key.Replace('_', ' ');
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving permission key: {key}. Ex: {ex.Message}");
                return key.Replace('_', ' ');
            }
        }
    }
}
