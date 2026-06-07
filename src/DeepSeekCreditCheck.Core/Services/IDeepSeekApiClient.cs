using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public interface IDeepSeekApiClient
{
    Task<BalanceSnapshot> GetBalanceAsync(string apiKey);
    Task<UsageRecord?> GetUsageAsync(string apiKey, DateTime? since = null, DateTime? until = null);
}
