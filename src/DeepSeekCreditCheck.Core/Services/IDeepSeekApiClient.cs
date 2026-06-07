using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public interface IDeepSeekApiClient
{
    Task<BalanceSnapshot> GetBalanceAsync(string apiKey);
}
