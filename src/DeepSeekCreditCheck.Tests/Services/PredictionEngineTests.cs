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
}
