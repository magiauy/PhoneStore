using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PhoneStore.Services.Interfaces;
using Windows.Storage;

namespace PhoneStore.Services.Implementations
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly StorageFolder _localFolder;

        public LocalStorageService()
        {
            _localFolder = ApplicationData.Current.LocalFolder;
        }

        public async Task<T?> GetItemAsync<T>(string key)
        {
            try
            {
                var file = await _localFolder.GetFileAsync($"{key}.json");
                var json = await FileIO.ReadTextAsync(file);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch (FileNotFoundException)
            {
                return default(T);
            }
            catch (Exception)
            {
                return default(T);
            }
        }

        public async Task SetItemAsync<T>(string key, T value)
        {
            try
            {
                var file = await _localFolder.CreateFileAsync($"{key}.json", CreationCollisionOption.ReplaceExisting);
                var json = JsonSerializer.Serialize(value);
                await FileIO.WriteTextAsync(file, json);
            }
            catch (Exception ex)
            {
                // Log error if needed
                throw new InvalidOperationException($"Failed to save item with key '{key}'", ex);
            }
        }

        public async Task RemoveItemAsync(string key)
        {
            try
            {
                var file = await _localFolder.GetFileAsync($"{key}.json");
                await file.DeleteAsync();
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist, nothing to remove
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to remove item with key '{key}'", ex);
            }
        }

        public async Task ClearAsync()
        {
            try
            {
                var files = await _localFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    if (file.Name.EndsWith(".json"))
                    {
                        await file.DeleteAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to clear local storage", ex);
            }
        }
    }
}