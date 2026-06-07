using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class PredictionEngine
{
    /// <summary>
    /// Spočítá předpokládaný počet dní, na který kredit vydrží.
    /// </summary>
    /// <param name="history">Historie balance snapshotů, seřazená vzestupně dle času.</param>
    /// <param name="currentBalance">Aktuální total balance v USD.</param>
    /// <returns>Predikce s počtem dní a denním průměrem.</returns>
    public PredictionResult Calculate(IReadOnlyList<BalanceSnapshot> history, decimal currentBalance)
    {
        if (history.Count < 2 || currentBalance <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        // Seřadit vzestupně
        var sorted = history.OrderBy(h => h.Timestamp).ToList();

        // Najít snapshoty s různými dny pro výpočet denní spotřeby
        var firstSnapshot = sorted.First();
        var lastSnapshot = sorted.Last();

        var totalSpend = firstSnapshot.TotalBalanceDecimal - lastSnapshot.TotalBalanceDecimal;
        var totalDays = (lastSnapshot.Timestamp - firstSnapshot.Timestamp).TotalDays;

        // Pokud data pokrývají méně než 1 hodinu, predikce není spolehlivá
        if (totalDays < 0.04) // ~1 hodina
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var avgDailySpend = totalSpend / (decimal)totalDays;

        if (avgDailySpend <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var daysRemaining = currentBalance / avgDailySpend;

        // Výpočet volatility pro pásmovou predikci
        var dailySpends = new List<decimal>();
        for (int i = 1; i < sorted.Count; i++)
        {
            var dayDiff = (sorted[i].Timestamp - sorted[i - 1].Timestamp).TotalDays;
            if (dayDiff > 0.001)
            {
                var spend = (sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal) / (decimal)dayDiff;
                if (spend >= 0) dailySpends.Add(spend);
            }
        }

        var isReliable = dailySpends.Count >= 3;
        decimal? rangeLow = null;
        decimal? rangeHigh = null;

        if (isReliable && dailySpends.Count > 1)
        {
            var mean = dailySpends.Average();
            var sumOfSquares = dailySpends.Sum(d => (d - mean) * (d - mean));
            var stdDev = (decimal)Math.Sqrt((double)(sumOfSquares / dailySpends.Count));

            if (stdDev > 0.3m * mean) // Vysoká volatilita → pásmo
            {
                rangeLow = currentBalance / (mean + stdDev);
                rangeHigh = currentBalance / Math.Max(mean - stdDev, 0.001m);
            }
        }

        return new PredictionResult
        {
            DaysRemaining = daysRemaining,
            RangeLow = rangeLow,
            RangeHigh = rangeHigh,
            AvgDailySpend = avgDailySpend,
            IsReliable = isReliable
        };
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
                return $"~{RangeLow.Value:F0}-{RangeHigh.Value:F0} dní";
            if (DaysRemaining.Value > 365) return "> 1 rok";
            if (DaysRemaining.Value > 30) return $"~{DaysRemaining.Value / 30:F0} měsíců";
            return $"~{DaysRemaining.Value:F0} dní";
        }
    }

    public string FormattedDailySpend =>
        AvgDailySpend > 0 ? $"${AvgDailySpend:F2}/den" : "—";
}
