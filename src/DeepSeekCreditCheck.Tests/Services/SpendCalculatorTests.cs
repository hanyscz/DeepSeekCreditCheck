using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class SpendCalculatorTests
{
    [Fact]
    public void EmptyList_ReturnsZero()
    {
        var result = SpendCalculator.SumPositiveDeltas(new List<BalanceSnapshot>());
        Assert.Equal(0, result);
    }

    [Fact]
    public void SingleSnapshot_ReturnsZero()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "100.00" }
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AllDecreasing_ReturnsSumOfPositiveDeltas()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "90.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 16, 0, 0, DateTimeKind.Utc), TotalBalance = "80.00" },
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(20.00m, result);
    }

    [Fact]
    public void WithTopUpInMiddle_CountsOnlyDecreases()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "90.00" },  // spend 10
            new() { Timestamp = new DateTime(2026, 6, 9, 14, 0, 0, DateTimeKind.Utc), TotalBalance = "110.00" }, // top-up +20, ignored
            new() { Timestamp = new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" }, // spend 10
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(20.00m, result);
    }

    [Fact]
    public void MultipleTopUps_CountsOnlyDecreases()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 10, 0, 0, DateTimeKind.Utc), TotalBalance = "80.00" },  // spend 20
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "150.00" }, // top-up +70
            new() { Timestamp = new DateTime(2026, 6, 9, 14, 0, 0, DateTimeKind.Utc), TotalBalance = "130.00" }, // spend 20
            new() { Timestamp = new DateTime(2026, 6, 9, 16, 0, 0, DateTimeKind.Utc), TotalBalance = "200.00" }, // top-up +70
            new() { Timestamp = new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc), TotalBalance = "180.00" }, // spend 20
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(60.00m, result);
    }

    [Fact]
    public void OnlyTopUps_ReturnsZero()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "80.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "90.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 16, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(0, result);
    }

    [Fact]
    public void AllSameBalance_ReturnsZero()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 16, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(0, result);
    }

    [Fact]
    public void PreOrderedInput_ReturnsCorrectTotal()
    {
        // Data už jsou seřazená od volajícího — SumPositiveDeltas je bere tak, jak jsou.
        var ordered = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "90.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 16, 0, 0, DateTimeKind.Utc), TotalBalance = "80.00" },
        };

        var result = SpendCalculator.SumPositiveDeltas(ordered);
        Assert.Equal(20.00m, result);
    }

    [Fact]
    public void SingleDecrease_ReturnsThatDelta()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "200.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc), TotalBalance = "50.00" },
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(150.00m, result);
    }


    [Fact]
    public void DecreasingWithZeroDeltaInMiddle_CountsCorrectly()
    {
        var snapshots = new List<BalanceSnapshot>
        {
            new() { Timestamp = new DateTime(2026, 6, 9, 8, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" },
            new() { Timestamp = new DateTime(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc), TotalBalance = "100.00" }, // zero delta, ignored
            new() { Timestamp = new DateTime(2026, 6, 9, 18, 0, 0, DateTimeKind.Utc), TotalBalance = "80.00" }, // spend 20
        };
        var result = SpendCalculator.SumPositiveDeltas(snapshots);
        Assert.Equal(20.00m, result);
    }
}
