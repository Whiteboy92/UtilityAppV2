using System.IO;
using System.Text.Json;
using UtilityAppV2.Services.Interfaces;
using UtilityAppV2.Settings;

namespace UtilityAppV2.Services.Implementation;

public class SettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string settingsFilePath;
    private readonly SemaphoreSlim gate = new(1, 1);

    public SettingsService()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string appFolder = Path.Combine(appData, "UtilityAppV2");
        Directory.CreateDirectory(appFolder);

        settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<UserSettings> LoadSettingsAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (!File.Exists(settingsFilePath))
                return new UserSettings();

            string json = await File.ReadAllTextAsync(settingsFilePath);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch (JsonException)
        {
            // Corrupt settings file — fall back to defaults rather than crashing.
            return new UserSettings();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task SaveSettingsAsync(UserSettings settings)
    {
        string json = JsonSerializer.Serialize(settings, JsonOptions);

        await gate.WaitAsync();
        try
        {
            // Write to a temp file then move into place so a crash mid-write
            // can never leave a truncated/corrupt settings file.
            string tempPath = settingsFilePath + ".tmp";
            await File.WriteAllTextAsync(tempPath, json);
            File.Move(tempPath, settingsFilePath, overwrite: true);
        }
        finally
        {
            gate.Release();
        }
    }
}
