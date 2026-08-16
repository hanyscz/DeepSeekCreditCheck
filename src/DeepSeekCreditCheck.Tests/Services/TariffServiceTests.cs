using System;
using DeepSeekCreditCheck.Core.Services;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class TariffServiceTests
{
    [Theory]
    [InlineData(0, 0, 0, false)]      // 00:00:00 UTC - Off-Peak
    [InlineData(0, 59, 59, false)]    // 00:59:59 UTC - Off-Peak
    [InlineData(1, 0, 0, true)]       // 01:00:00 UTC - Peak 1 start
    [InlineData(2, 30, 0, true)]      // 02:30:00 UTC - Peak 1 middle
    [InlineData(3, 59, 59, true)]     // 03:59:59 UTC - Peak 1 end
    [InlineData(4, 0, 0, false)]      // 04:00:00 UTC - Off-Peak
    [InlineData(5, 0, 0, false)]      // 05:00:00 UTC - Off-Peak
    [InlineData(5, 59, 59, false)]    // 05:59:59 UTC - Off-Peak
    [InlineData(6, 0, 0, true)]       // 06:00:00 UTC - Peak 2 start
    [InlineData(8, 15, 0, true)]      // 08:15:00 UTC - Peak 2 middle
    [InlineData(9, 59, 59, true)]     // 09:59:59 UTC - Peak 2 end
    [InlineData(10, 0, 0, false)]     // 10:00:00 UTC - Off-Peak
    [InlineData(16, 0, 0, false)]     // 16:00:00 UTC - Off-Peak
    [InlineData(23, 59, 59, false)]   // 23:59:59 UTC - Off-Peak
    public void IsPeak_EvaluatesCorrectly(int hour, int minute, int second, bool expectedPeak)
    {
        var testUtc = new DateTime(2026, 8, 16, hour, minute, second, DateTimeKind.Utc);
        var result = TariffService.IsPeak(testUtc);
        Assert.Equal(expectedPeak, result);
    }

    [Fact]
    public void GetTariffInfo_DuringPeak1_ReturnsCorrectTransitionAndRemaining()
    {
        var testUtc = new DateTime(2026, 8, 16, 2, 15, 0, DateTimeKind.Utc);
        var info = TariffService.GetTariffInfo(testUtc);

        Assert.True(info.IsPeak);
        Assert.Equal(TariffType.Peak, info.TariffType);
        Assert.Equal(new DateTime(2026, 8, 16, 4, 0, 0, DateTimeKind.Utc), info.TransitionUtc);
        Assert.Equal(TimeSpan.FromMinutes(105), info.Remaining);
        Assert.Equal("za 1 h 45 min", info.FormattedRemaining);
    }

    [Fact]
    public void GetTariffInfo_DuringOffPeakEarlyMorning_ReturnsPeak1Start()
    {
        var testUtc = new DateTime(2026, 8, 16, 0, 35, 0, DateTimeKind.Utc);
        var info = TariffService.GetTariffInfo(testUtc);

        Assert.False(info.IsPeak);
        Assert.Equal(TariffType.OffPeak, info.TariffType);
        Assert.Equal(new DateTime(2026, 8, 16, 1, 0, 0, DateTimeKind.Utc), info.TransitionUtc);
        Assert.Equal(TimeSpan.FromMinutes(25), info.Remaining);
        Assert.Equal("za 25 min", info.FormattedRemaining);
    }

    [Fact]
    public void GetTariffInfo_DuringOffPeakBetweenPeaks_ReturnsPeak2Start()
    {
        var testUtc = new DateTime(2026, 8, 16, 4, 30, 0, DateTimeKind.Utc);
        var info = TariffService.GetTariffInfo(testUtc);

        Assert.False(info.IsPeak);
        Assert.Equal(TariffType.OffPeak, info.TariffType);
        Assert.Equal(new DateTime(2026, 8, 16, 6, 0, 0, DateTimeKind.Utc), info.TransitionUtc);
        Assert.Equal(TimeSpan.FromMinutes(90), info.Remaining);
        Assert.Equal("za 1 h 30 min", info.FormattedRemaining);
    }

    [Fact]
    public void GetTariffInfo_DuringPeak2_ReturnsPeak2End()
    {
        var testUtc = new DateTime(2026, 8, 16, 8, 0, 0, DateTimeKind.Utc);
        var info = TariffService.GetTariffInfo(testUtc);

        Assert.True(info.IsPeak);
        Assert.Equal(TariffType.Peak, info.TariffType);
        Assert.Equal(new DateTime(2026, 8, 16, 10, 0, 0, DateTimeKind.Utc), info.TransitionUtc);
        Assert.Equal(TimeSpan.FromHours(2), info.Remaining);
        Assert.Equal("za 2 h 0 min", info.FormattedRemaining);
    }

    [Fact]
    public void GetTariffInfo_DuringOffPeakEvening_ReturnsNextDayPeak1Start()
    {
        var testUtc = new DateTime(2026, 8, 16, 14, 0, 0, DateTimeKind.Utc);
        var info = TariffService.GetTariffInfo(testUtc);

        Assert.False(info.IsPeak);
        Assert.Equal(TariffType.OffPeak, info.TariffType);
        Assert.Equal(new DateTime(2026, 8, 17, 1, 0, 0, DateTimeKind.Utc), info.TransitionUtc);
        Assert.Equal(TimeSpan.FromHours(11), info.Remaining);
        Assert.Equal("za 11 h 0 min", info.FormattedRemaining);
    }
}
