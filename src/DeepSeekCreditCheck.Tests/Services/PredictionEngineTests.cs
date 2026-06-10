using System.Globalization;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class PredictionEngineTests
{
    public PredictionEngineTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    [Fact]
    public void Calculate_WithSufficientHistory_ReturnsReliablePrediction()
    {
        var engine = new PredictionEngine();
        var now = DateTime.UtcNow;

        // 7 dní, každý den 2 snapshoty (ráno/večer), spotřeba ~$0.7/den
        var history = new List<BalanceSnapshot>();
        var bal = 110m;
        for (int d = 7; d >= 0; d--)
        {
            var dayStart = now.AddDays(-d).Date.AddHours(8);
            history.Add(new BalanceSnapshot { Timestamp = dayStart, TotalBalance = bal.ToString("F2", CultureInfo.InvariantCulture) });
            bal -= 0.5m + (decimal)(d % 3) * 0.2m;
            history.Add(new BalanceSnapshot { Timestamp = dayStart.AddHours(12), TotalBalance = bal.ToString("F2", CultureInfo.InvariantCulture) });
        }

        var result = engine.Calculate(history, bal);

        Assert.True(result.IsReliable);
        Assert.True(result.DaysRemaining > 0);
        Assert.True(result.AvgDailySpend > 0);
    }

    [Fact]
    public void Calculate_WithSingleSnapshot_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "50.00" }
        };

        var result = engine.Calculate(history, 50.00m);

        Assert.False(result.IsReliable);
        Assert.Null(result.DaysRemaining);
        Assert.Equal("—", result.FormattedPrediction);
    }

    [Fact]
    public void Calculate_WithEmptyHistory_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var result = engine.Calculate(new List<BalanceSnapshot>(), 10.00m);
        Assert.False(result.IsReliable);
    }

    [Fact]
    public void Calculate_WithZeroBalance_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var now = DateTime.UtcNow;
        // 2 dny, 2 snapshoty na den — žádná spotřeba
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = now.AddDays(-1).Date.AddHours(8), TotalBalance = "10.00" },
            new() { Timestamp = now.AddDays(-1).Date.AddHours(20), TotalBalance = "10.00" },
            new() { Timestamp = now.Date.AddHours(8), TotalBalance = "10.00" },
            new() { Timestamp = now.Date.AddHours(20), TotalBalance = "10.00" },
        };

        var result = engine.Calculate(history, 0m);
        Assert.False(result.IsReliable);
        Assert.Null(result.DaysRemaining);
    }

    /// <summary>
    /// Test s reálnými daty z exportu (07.06.–10.06.2026).
    /// 07.06. je částečný den (~5h) → musí být vyřazen.
    /// 08.06. (~16h, s dobitím) → spotřeba ~1.04.
    /// 09.06. (~23h) → spotřeba ~0.69.
    /// 10.06. (dnešek) → vyřazen.
    /// Průměr: (1.04 + 0.69) / 2 = 0.865 → ~0.87/den.
    /// Predikce: 20.92 / 0.865 ≈ ~24 dní.
    /// </summary>
    [Fact]
    public void Calculate_WithRealWorldData_FiltersPartialDays()
    {
        var engine = new PredictionEngine();

        // Simulace: DateTime.UtcNow = 10.06.2026 08:29 UTC (= 10:29 local)
        // Použijeme pevné UTC časy pro reprodukovatelnost testu
        // 09.06. 22:00 UTC = 10.06. 00:00 local → používáme jako "dnešek" pro Today
        // Aby DateTime.Today vracel 10.06., musíme být v local time zóně UTC+2

        var history = new List<BalanceSnapshot>();

        // === 07.06.2026 (ČÁSTEČNÝ DEN: 17:59–22:59 local = 15:59–20:59 UTC, jen ~5h) ===
        // UTC časy:
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 7, 15, 59, 0, DateTimeKind.Utc), TotalBalance = "18.40" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 7, 18, 32, 0, DateTimeKind.Utc), TotalBalance = "18.25" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 7, 20, 59, 0, DateTimeKind.Utc), TotalBalance = "18.16" });

        // === 08.06.2026 (PLNÝ DEN: 07:28–23:59 local, ~16.5h, s dobitím) ===
        // UTC = local - 2h
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 5, 28, 0, DateTimeKind.Utc), TotalBalance = "18.13" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 9, 31, 0, DateTimeKind.Utc), TotalBalance = "18.01" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 15, 59, 0, DateTimeKind.Utc), TotalBalance = "17.42" });
        // Top-up: balance skočí z 17.42 na 22.42
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 18, 4, 0, DateTimeKind.Utc), TotalBalance = "22.42" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 20, 39, 0, DateTimeKind.Utc), TotalBalance = "22.42" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 21, 59, 0, DateTimeKind.Utc), TotalBalance = "22.12" });

        // === 09.06.2026 (PLNÝ DEN: 00:09–23:55 local, ~23.7h) ===
        // UTC = local - 2h
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 22, 9, 0, DateTimeKind.Utc), TotalBalance = "22.12" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 10, 4, 0, DateTimeKind.Utc), TotalBalance = "22.04" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 12, 54, 0, DateTimeKind.Utc), TotalBalance = "21.91" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 14, 36, 0, DateTimeKind.Utc), TotalBalance = "21.88" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 20, 45, 0, DateTimeKind.Utc), TotalBalance = "21.50" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 21, 55, 0, DateTimeKind.Utc), TotalBalance = "21.43" });

        // === 10.06.2026 (DNEŠEK — vynechán z průměru) ===
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 22, 5, 0, DateTimeKind.Utc), TotalBalance = "21.43" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 10, 7, 45, 0, DateTimeKind.Utc), TotalBalance = "20.92" });

        var currentBalance = 20.92m;
        var result = engine.Calculate(history, currentBalance);

        // Průměrná denní spotřeba: jen z plných dnů (08.06 a 09.06)
        // ~(1.04 + 0.69) / 2 ≈ 0.87
        Assert.True(result.AvgDailySpend > 0.80m && result.AvgDailySpend < 0.95m,
            $"AvgDailySpend should be ~0.87 but was {result.AvgDailySpend}");

        // Predikce: ~24 dní (20.92 / 0.87)
        Assert.True(result.DaysRemaining.HasValue);
        Assert.True(result.DaysRemaining.Value > 20 && result.DaysRemaining.Value < 30,
            $"DaysRemaining should be ~24 but was {result.DaysRemaining}");

        // Range by neměl být spočítán (máme jen 2 plné dny, potřebujeme ≥3)
        Assert.Null(result.RangeLow);
        Assert.Null(result.RangeHigh);

        // Predikce by měla být jen "~N dní" (bez rozsahu)
        Assert.DoesNotContain("-", result.FormattedPrediction);
    }

    [Fact]
    public void Calculate_WithThreeFullDays_ComputesRange()
    {
        var engine = new PredictionEngine();

        // 3 plné dny s RŮZNÝMI spotřebami (aby stdDev > 0).
        // UTC časy volíme tak, aby po převodu na lokální (UTC+2) zůstaly ve stejném dni.
        // 04:00 UTC = 06:00 local, 20:00 UTC = 22:00 local → rozpětí 16h (> 12h).

        var history = new List<BalanceSnapshot>();

        // Den 1: spend $2.00
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 7, 4, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 7, 20, 0, 0, DateTimeKind.Utc), TotalBalance = "98.00" });

        // Den 2: spend $1.50
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 4, 0, 0, DateTimeKind.Utc), TotalBalance = "98.00" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 8, 20, 0, 0, DateTimeKind.Utc), TotalBalance = "96.50" });

        // Den 3: spend $1.00
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 4, 0, 0, DateTimeKind.Utc), TotalBalance = "96.50" });
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 9, 20, 0, 0, DateTimeKind.Utc), TotalBalance = "95.50" });

        // Dnešek (vynechán z průměru)
        history.Add(new BalanceSnapshot { Timestamp = new DateTime(2026, 6, 10, 4, 0, 0, DateTimeKind.Utc), TotalBalance = "95.50" });

        var result = engine.Calculate(history, 95.50m);

        Assert.True(result.IsReliable);
        Assert.True(result.AvgDailySpend > 0);
        // 3 plné dny → range by měl být spočítán
        Assert.NotNull(result.RangeLow);
        Assert.NotNull(result.RangeHigh);
        Assert.Contains("-", result.FormattedPrediction);
    }
}
