using UtilityAppV2.Settings;

namespace UtilityAppV2.Services.Interfaces;

public interface ISettingsService
{
    Task<UserSettings> LoadSettingsAsync();
    Task SaveSettingsAsync(UserSettings settings);
}