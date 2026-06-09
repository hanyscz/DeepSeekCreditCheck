using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public static class SpendCalculator
{
    /// <summary>
    /// Computes total spend from balance snapshots by summing only positive
    /// consecutive deltas (balance decreases). This correctly handles top-ups
    /// within the period by ignoring balance increases.
    /// </summary>
    /// <param name="snapshots">Balance snapshots within the time window. Sorted internally by Timestamp.</param>
    /// <returns>Total spend (sum of all positive deltas), or 0 if fewer than 2 snapshots.</returns>
    public static decimal SumPositiveDeltas(IReadOnlyList<BalanceSnapshot> snapshots)
    {
        if (snapshots.Count < 2)
            return 0;

        var sorted = snapshots.OrderBy(h => h.Timestamp).ToList();

        decimal total = 0;
        for (int i = 1; i < sorted.Count; i++)
        {
            var delta = sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal;
            if (delta > 0)
                total += delta;
        }

        return total;
    }
}
