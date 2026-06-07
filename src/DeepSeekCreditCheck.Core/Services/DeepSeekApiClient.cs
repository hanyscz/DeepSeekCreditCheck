using System.Net.Http.Json;
using System.Text.Json;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class DeepSeekApiClient : IDeepSeekApiClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.deepseek.com";

    public DeepSeekApiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<BalanceSnapshot> GetBalanceAsync(string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/user/balance");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var isAvailable = json.GetProperty("is_available").GetBoolean();
        var infos = json.GetProperty("balance_infos");
        var info = infos[0];

        return new BalanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            IsAvailable = isAvailable,
            Currency = info.GetProperty("currency").GetString() ?? "USD",
            TotalBalance = info.GetProperty("total_balance").GetString() ?? "0.00",
            GrantedBalance = info.GetProperty("granted_balance").GetString() ?? "0.00",
            ToppedUpBalance = info.GetProperty("topped_up_balance").GetString() ?? "0.00"
        };
    }

    public async Task<UsageRecord?> GetUsageAsync(string apiKey, DateTime? since = null, DateTime? until = null)
    {
        var start = since?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var end = until?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/v1/usage?start_time={start}&end_time={end}");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);

        // Usage endpoint nemusí existovat — vracíme null, fallback výpočet
        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var total = json.GetProperty("total_usage");

            return new UsageRecord
            {
                Timestamp = DateTime.UtcNow,
                PeriodStart = since ?? DateTime.UtcNow.AddDays(-7),
                PeriodEnd = until ?? DateTime.UtcNow,
                TotalTokens = total.GetProperty("total_tokens").GetInt64(),
                InputTokens = total.GetProperty("input_tokens").GetInt64(),
                OutputTokens = total.GetProperty("output_tokens").GetInt64(),
                CachedTokens = total.TryGetProperty("cached_tokens", out var ct)
                    ? ct.GetInt64() : null
            };
        }
        catch
        {
            return null; // Neznámý formát odpovědi
        }
    }
}
