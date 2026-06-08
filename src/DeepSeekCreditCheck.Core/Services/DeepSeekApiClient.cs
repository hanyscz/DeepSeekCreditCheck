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
            TotalBalance = info.GetProperty("total_balance").GetString() ?? "0.00"
        };
    }
}
