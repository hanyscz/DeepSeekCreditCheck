using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IBalanceRepository
{
    Task SaveAsync(BalanceSnapshot snapshot);
    Task<BalanceSnapshot?> GetLatestAsync(string? currency = "USD");
    Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until, string? currency = "USD");
    Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100, string? currency = "USD");
    Task DeleteAsync(IEnumerable<int> ids);
}
