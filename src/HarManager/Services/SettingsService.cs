using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HarManager.Services
{
    public class AppSettings
    {
        public string ProviderName { get; set; } = "WebDAV";
        public string BaseUrl { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public static class SettingsService
    {
        private static string SettingsPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HarManager_settings.json");

        public static async Task SaveSettingsAsync(AppSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings);
                await File.WriteAllTextAsync(SettingsPath, json);
            }
            catch
            {
                // Ignore errors for now
            }
        }

        public static async Task<AppSettings> LoadSettingsAsync()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var json = await File.ReadAllTextAsync(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // Ignore errors
            }
            return new AppSettings();
        }
    }
}

