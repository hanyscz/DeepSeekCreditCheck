using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class TrayIconFormatterTests
{
    [Theory]
    [InlineData(null, 2.0, BalanceStatus.Unknown)]
    [InlineData(1.99, 2.0, BalanceStatus.Critical)]   // pod prahem
    [InlineData(2.0, 2.0, BalanceStatus.Warning)]     // = práh → varování
    [InlineData(3.5, 2.0, BalanceStatus.Warning)]     // <= 2× práh
    [InlineData(4.0, 2.0, BalanceStatus.Warning)]     // = 2× práh
    [InlineData(4.01, 2.0, BalanceStatus.Ok)]         // > 2× práh
    [InlineData(100.0, 2.0, BalanceStatus.Ok)]
    public void GetStatus_ReturnsExpected(double? balance, double threshold, BalanceStatus expected)
    {
        var result = TrayIconFormatter.GetStatus(
            balance.HasValue ? (decimal)balance.Value : null, (decimal)threshold);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(null, "?")]
    [InlineData(0.0, "0")]
    [InlineData(1.5, "1.5")]
    [InlineData(9.94, "9.9")]
    [InlineData(10.0, "10")]
    [InlineData(42.7, "42")]
    [InlineData(99.9, "99")]
    [InlineData(100.0, "99+")]
    [InlineData(1234.5, "99+")]
    [InlineData(-5.0, "0")]
    public void GetIconText_FormatsBalance(double? balance, string expected)
    {
        var result = TrayIconFormatter.GetIconText(
            balance.HasValue ? (decimal)balance.Value : null);

        Assert.Equal(expected, result);
    }
}
