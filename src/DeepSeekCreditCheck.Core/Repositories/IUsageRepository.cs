using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IUsageRepository
{
    Task SaveAsync(UsageRecord record);
    Task<IReadOnlyList<UsageRecord>> GetHistoryAsync(DateTime since, DateTime until);
    Task<UsageRecord?> GetLatestAsync();
}
