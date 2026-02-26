using System.IO;
using System.Text.Json;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.Settings;

namespace UtilityAppV2.Services.Implementation;

public class SettingsService : ISettingsService
{
    private readonly string settingsFilePath;

    public SettingsService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appData, "UtilityAppV2");
        Directory.CreateDirectory(appFolder);

        settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<UserSettings> LoadSettingsAsync()
    {
        if (!File.Exists(settingsFilePath))
            return new UserSettings();

        string json = await File.ReadAllTextAsync(settingsFilePath);
        return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(settingsFilePath, json);
    }
}