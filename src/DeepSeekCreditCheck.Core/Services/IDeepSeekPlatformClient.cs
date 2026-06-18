using System.Text.Json.Nodes;

namespace DeepSeekCreditCheck.Core.Services;

public interface IDeepSeekPlatformClient : IDisposable
{
    Task<JsonNode?> GetJsonAsync(string path, string sessionToken, Dictionary<string, string>? queryParams = null);
    Task<JsonNode?> GetUserSummaryAsync(string sessionToken);
    Task<JsonNode?> GetUsageAmountAsync(string sessionToken, int year, int month);
    Task<JsonNode?> GetUsageCostAsync(string sessionToken, int year, int month);
    Task<byte[]> GetUsageExportZipAsync(string sessionToken, int year, int month);
}
