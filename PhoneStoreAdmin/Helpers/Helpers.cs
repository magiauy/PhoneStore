using System;
using System.Collections.Concurrent;
using Microsoft.Windows.ApplicationModel.Resources;
using PhoneStoreAdmin.Utils;

namespace PhoneStoreAdmin.Helpers
{
    public static class LocalizationHelper
    {
        private static readonly ResourceLoader _resourceLoader = new(); // Mặc định Resources.resw
        private static readonly ConcurrentDictionary<string, ResourceLoader> _resourceLoaders = new();

        /// <summary>
        /// Lấy chuỗi từ các resource theo section. Mặc định sử dụng Resources.resw
        /// </summary>
        public static string GetString(string key, string section = "Resources")
        {
            if (string.IsNullOrWhiteSpace(section))
            {
                section = "Resources";
            }

            try
            {
                var loader = GetResourceLoader(section);
                string value = loader.GetString(key);
                return string.IsNullOrEmpty(value) ? key : value;
            }
            catch (Exception ex)
            {
                Logger.Error($"Missing localization key: {key} in section {section}, ex: {ex.Message}");
                return key;
            }
        }

        /// <summary>
        /// Lấy chuỗi từ Permission.resw
        /// </summary>
        public static string GetPermissionString(string key)
        {
            string value = GetString(key, "Permission");
            return value.Equals(key, StringComparison.Ordinal) ? key.Replace('_', ' ') : value;
        }

        private static ResourceLoader GetResourceLoader(string section)
        {
            if (section.Equals("Resources", StringComparison.OrdinalIgnoreCase))
            {
                return _resourceLoader;
            }

            return _resourceLoaders.GetOrAdd(section, static s => new ResourceLoader($"Resources/{s}"));
        }
    }
}
