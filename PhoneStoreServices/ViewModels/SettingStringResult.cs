using PhoneStoreRepository.Models;
using System.Collections.Generic;

namespace PhoneStore.Services.ViewModels
{
    /// <summary>
    /// Result wrapper for paginated setting string queries
    /// </summary>
    public class SettingStringResult
    {
        public IEnumerable<SettingString> Settings { get; }
        public InfoTable Info { get; }

        public SettingStringResult(IEnumerable<SettingString> settings, InfoTable info)
        {
            Settings = settings;
            Info = info;
        }
    }
}
