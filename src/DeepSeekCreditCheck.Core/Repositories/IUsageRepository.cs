using System.Collections.Generic;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IUsageRepository
{
    Task SaveUsageDetailsAsync(int year, int month, List<UsageDetailSnapshot> details);
    Task<IReadOnlyList<UsageDetailSnapshot>> GetUsageDetailsAsync(int year, int month);
    Task<IReadOnlyList<MonthlyTotal>> GetMonthlyTotalsAsync();
    Task DeleteUsageDetailsAsync(int year, int month);
}
