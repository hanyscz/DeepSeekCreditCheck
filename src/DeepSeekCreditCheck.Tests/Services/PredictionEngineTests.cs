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
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-7), TotalBalance = "100.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-5), TotalBalance = "90.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-3), TotalBalance = "82.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-1), TotalBalance = "75.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "70.00" }
        };

        var result = engine.Calculate(history, 70.00m);

        Assert.True(result.IsReliable);
        Assert.True(result.DaysRemaining > 0);
        Assert.True(result.AvgDailySpend > 0);
        // ~30 spent over 7 days => ~4.29/day => ~16 days remaining
        Assert.True(result.DaysRemaining > 10 && result.DaysRemaining < 25);
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
    public void FormattedPrediction_Over365Days_ShowsMonths()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-2), TotalBalance = "100.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "99.99" }
        };

        var result = engine.Calculate(history, 100.00m);
        Assert.Contains("rok", result.FormattedPrediction);
    }

    [Fact]
    public void Calculate_WithZeroBalance_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-1), TotalBalance = "10.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "0.00" }
        };

        var result = engine.Calculate(history, 0m);
        Assert.False(result.IsReliable);
        Assert.Null(result.DaysRemaining);
    }

    [Fact]
    public void FormattedPrediction_Over30Days_ShowsMonths()
    {
        var engine = new PredictionEngine();
        // 100 drop to 80 over 12 days => avgDailySpend = 20/12 ≈ 1.667
        // daysRemaining = 100 / 1.667 ≈ 60, which is > 30 and < 365
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-12), TotalBalance = "100.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "80.00" }
        };

        var result = engine.Calculate(history, 100.00m);
        // Should show months since > 30 days but < 365 days
        Assert.Contains("měsíců", result.FormattedPrediction);
    }
}
