using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class PredictionEngine
{
    public PredictionResult Calculate(IReadOnlyList<BalanceSnapshot> history, decimal currentBalance)
    {
        if (history.Count < 2 || currentBalance <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var sorted = history.OrderBy(h => h.Timestamp).ToList();

        // Agregovat denní spotřebu po kalendářních dnech
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

            if (stdDev > 0.3m * avgDailySpend && stdDev < avgDailySpend * 2)
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
            .GroupBy(h => h.Timestamp.Date) // UTC day
            .OrderBy(g => g.Key)
            .ToList();

        var result = new List<DaySpend>();
        for (int i = 0; i < groups.Count - 1; i++)
        {
            var todayGroup = groups[i];
            var tomorrowGroup = groups[i + 1];

            // Vzít první snapshot dnešního dne a poslední snapshot zítřka (nebo první zítřek)
            var todayBalance = todayGroup.First().TotalBalanceDecimal;

            // Najít nejbližší snapshot další den
            var nextDayBalance = tomorrowGroup.First().TotalBalanceDecimal;
            var daysDiff = (tomorrowGroup.Key - todayGroup.Key).Days;

            if (daysDiff <= 0) continue;

            var spend = todayBalance - nextDayBalance;
            if (spend < 0) continue; // dobití kreditu

            // Rozpustit do mezer — pokud chybí den, rozpočítat poměrně
            var spendPerDay = spend / daysDiff;

            for (int d = 0; d < daysDiff; d++)
            {
                if (d < daysDiff) // všechny dny kromě posledního (ten patří dalšímu intervalu)
                    result.Add(new DaySpend { Day = todayGroup.Key.AddDays(d), Spend = spendPerDay });
            }
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
                // Pokud je pásmo příliš široké (>5x), zobrazit jen střední hodnotu
                if (rh > rl * 5) return $"~{DaysRemaining.Value:F0} dní";
                return $"~{rl}-{rh} dní";
            }
            if (DaysRemaining.Value > 365) return "> 1 rok";
            if (DaysRemaining.Value > 30) return $"~{DaysRemaining.Value / 30:F0} měsíců";
            return $"~{DaysRemaining.Value:F0} dní";
        }
    }

    public string FormattedDailySpend =>
        AvgDailySpend > 0 ? $"${AvgDailySpend:F2}/den" : "—";
}
