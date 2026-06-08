namespace DeepSeekCreditCheck.Core.Services;

public interface IAppSettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<string?> GetApiKeyAsync();
    Task SetApiKeyAsync(string apiKey);
    Task<string?> GetAlertThresholdAsync();
    Task SetAlertThresholdAsync(string threshold);
    Task<int> GetPollingIntervalMinutesAsync();
    Task SetPollingIntervalMinutesAsync(int minutes);
    Task<string?> GetLanguageAsync();
    Task SetLanguageAsync(string lang);
    Task<string?> GetLogPathAsync();
    Task SetLogPathAsync(string path);
    Task<string?> GetDbPathAsync();
    Task SetDbPathAsync(string path);
}
