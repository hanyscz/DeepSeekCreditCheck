using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class PredictionEngine
{
    public PredictionResult Calculate(IReadOnlyList<BalanceSnapshot> history, decimal currentBalance)
    {
        if (history.Count < 2 || currentBalance <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var sorted = history.OrderBy(h => h.Timestamp).ToList();

        // Agregovat denní spotřebu po kalendářních dnech (UTC)
        // Každý den se spočítá rozdíl: první hodnota dne - poslední hodnota dne
        var daySpends = AggregateDailySpend(sorted);

        if (daySpends.Count < 1)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var totalSpend = daySpends.Sum(d => d.Spend);
        var dayCount = daySpends.Count;

        var avgDailySpend = (decimal)dayCount > 0 ? totalSpend / (decimal)dayCount : 0;

        if (avgDailySpend <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var daysRemaining = currentBalance / avgDailySpend;

        // Výpočet volatility z denních hodnot
        var isReliable = dayCount >= 2;
        decimal? rangeLow = null;
        decimal? rangeHigh = null;

        if (isReliable && dayCount > 1)
        {
            var mean = (double)avgDailySpend;
            var sumOfSquares = daySpends.Sum(d => Math.Pow((double)d.Spend - mean, 2));
            var stdDev = (decimal)Math.Sqrt(sumOfSquares / dayCount);

            if (stdDev > 0.01m && stdDev < avgDailySpend * 2)
            {
                rangeLow = currentBalance / Math.Max(avgDailySpend + stdDev, 0.001m);
                rangeHigh = currentBalance / Math.Max(avgDailySpend - stdDev, 0.001m);
            }
        }

        var result = new PredictionResult
        {
            DaysRemaining = daysRemaining,
            RangeLow = rangeLow,
            RangeHigh = rangeHigh,
            AvgDailySpend = avgDailySpend,
            IsReliable = isReliable
        };

        return result;
    }

    private static List<DaySpend> AggregateDailySpend(List<BalanceSnapshot> sorted)
    {
        // Seskupit snapshoty po kalendářních dnech (UTC)
        var groups = sorted
            .GroupBy(h => h.Timestamp.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var result = new List<DaySpend>();

        // Každý den: první snapshot ráno vs poslední snapshot večer = denní spotřeba
        foreach (var group in groups)
        {
            var ordered = group.OrderBy(h => h.Timestamp).ToList();
            var firstBalance = ordered.First().TotalBalanceDecimal;
            var lastBalance = ordered.Last().TotalBalanceDecimal;
            var spend = firstBalance - lastBalance;

            if (spend > 0)
                result.Add(new DaySpend { Day = group.Key, Spend = spend });
        }

        // Omezit na posledních 90 dní
        var cutoff = DateTime.UtcNow.Date.AddDays(-90);
        return result.Where(d => d.Day >= cutoff).TakeLast(90).ToList();
    }

    private class DaySpend
    {
        public DateTime Day { get; init; }
        public decimal Spend { get; init; }
    }
}

public class PredictionResult
{
    public decimal? DaysRemaining { get; set; }
    public decimal? RangeLow { get; set; }
    public decimal? RangeHigh { get; set; }
    public decimal AvgDailySpend { get; set; }
    public bool IsReliable { get; set; }

    public string FormattedPrediction
    {
        get
        {
            if (!DaysRemaining.HasValue) return "—";
            if (RangeLow.HasValue && RangeHigh.HasValue)
            {
                var rl = Math.Round(RangeLow.Value, 0);
                var rh = Math.Round(RangeHigh.Value, 0);
                if (rh > rl * 5) return $"~{DaysRemaining.Value:F0} dní";
                return $"~{rl}-{rh} dní";
            }
            return $"~{DaysRemaining.Value:F0} dní";
        }
    }

    public string FormattedDailySpend =>
        AvgDailySpend > 0 ? $"${AvgDailySpend:F2}/den" : "—";
}
