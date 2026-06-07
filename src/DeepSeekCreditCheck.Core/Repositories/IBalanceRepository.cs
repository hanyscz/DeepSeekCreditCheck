using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IBalanceRepository
{
    Task SaveAsync(BalanceSnapshot snapshot);
    Task<BalanceSnapshot?> GetLatestAsync();
    Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until);
    Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100);
}
