using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Windows.ApplicationModel.Resources;

namespace PhoneStoreAdmin.Helpers
{
    public static class LocalizationHelper
    {
        private static readonly ResourceLoader _resourceLoader = new();

        public static string GetString(string key)
        {
            return _resourceLoader.GetString(key);
        }
    }
}
