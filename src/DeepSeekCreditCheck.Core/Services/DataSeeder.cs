using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;

namespace DeepSeekCreditCheck.Core.Services;

public class DataSeeder
{
    private readonly IBalanceRepository _balanceRepo;

    public DataSeeder(IBalanceRepository balanceRepo)
    {
        _balanceRepo = balanceRepo;
    }

    public async Task SeedAsync(int days)
    {
        var existing = await _balanceRepo.GetLatestAsync();
        var startAmount = existing != null
            ? existing.TotalBalanceDecimal
            : 110m;

        var rng = new Random(42);
        var snapshots = new List<BalanceSnapshot>();
        var now = DateTime.UtcNow;

        for (int i = days; i >= 0; i--)
        {
            // Denní útrata mezi $1 a $8
            var dailySpend = (decimal)(1 + rng.NextDouble() * 7);
            var amount = startAmount - dailySpend * (days - i) / days;
            if (amount < 0) amount = 0;

            snapshots.Add(new BalanceSnapshot
            {
                Timestamp = now.AddDays(-i).AddHours(rng.Next(8, 22)).AddMinutes(rng.Next(0, 59)),
                IsAvailable = amount > 0,
                Currency = "USD",
                TotalBalance = amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                GrantedBalance = (amount * 0.1m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                ToppedUpBalance = (amount * 0.9m).ToString("F2", System.Globalization.CultureInfo.InvariantCulture)
            });
        }

        foreach (var s in snapshots)
            await _balanceRepo.SaveAsync(s);
    }
}
