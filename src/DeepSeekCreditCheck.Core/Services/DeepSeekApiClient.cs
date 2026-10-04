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

    public async Task<IReadOnlyList<BalanceSnapshot>> GetAllBalancesAsync(string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/user/balance");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var infos = json.GetProperty("balance_infos");
        var now = DateTime.UtcNow;

        var list = new List<BalanceSnapshot>();
        foreach (var info in infos.EnumerateArray())
        {
            list.Add(new BalanceSnapshot
            {
                Timestamp = now,
                Currency = info.GetProperty("currency").GetString() ?? "USD",
                TotalBalance = info.GetProperty("total_balance").GetString() ?? "0.00"
            });
        }

        return list;
    }

    public async Task<BalanceSnapshot> GetBalanceAsync(string apiKey)
    {
        var balances = await GetAllBalancesAsync(apiKey);
        if (balances.Count == 0)
        {
            return new BalanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Currency = "USD",
                TotalBalance = "0.00"
            };
        }

        // Vždy preferujeme USD pro výpočty a zobrazení
        return balances.FirstOrDefault(b => string.Equals(b.Currency, "USD", StringComparison.OrdinalIgnoreCase))
               ?? balances[0];
    }
}
