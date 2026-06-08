using Dapper;
using DeepSeekCreditCheck.Core.Configuration;
using DeepSeekCreditCheck.Core.Data;

namespace DeepSeekCreditCheck.Core.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly AppDbContext _db;

    public AppSettingsService(AppDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT Value FROM AppSettings WHERE Key = @key", new { key });
    }

    public async Task SetAsync(string key, string value)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT OR REPLACE INTO AppSettings (Key, Value) VALUES (@key, @value)",
            new { key, value });
    }

    public async Task<string?> GetApiKeyAsync()
    {
        var encrypted = await GetAsync("ApiKey");
        if (string.IsNullOrEmpty(encrypted)) return null;
        try { return DataProtection.Unprotect(encrypted); }
        catch { return null; }
    }

    public async Task SetApiKeyAsync(string apiKey)
    {
        var encrypted = DataProtection.Protect(apiKey);
        await SetAsync("ApiKey", encrypted);
    }

    public async Task<string?> GetAlertThresholdAsync()
        => await GetAsync("AlertThreshold") ?? "2.00";

    public async Task SetAlertThresholdAsync(string threshold)
        => await SetAsync("AlertThreshold", threshold);

    public async Task<int> GetPollingIntervalMinutesAsync()
    {
        var val = await GetAsync("PollingIntervalMin");
        return int.TryParse(val, out var i) ? i : 15;
    }

    public async Task SetPollingIntervalMinutesAsync(int minutes)
        => await SetAsync("PollingIntervalMin", minutes.ToString());

    public async Task<string?> GetLanguageAsync()
        => await GetAsync("Language") ?? "cs";

    public async Task SetLanguageAsync(string lang)
        => await SetAsync("Language", lang);

    public async Task<string?> GetLogPathAsync()
        => await GetAsync("LogPath");

    public async Task SetLogPathAsync(string path)
        => await SetAsync("LogPath", path);

    public async Task<string?> GetDbPathAsync()
        => await GetAsync("DbPath");

    public async Task SetDbPathAsync(string path)
        => await SetAsync("DbPath", path);
}
