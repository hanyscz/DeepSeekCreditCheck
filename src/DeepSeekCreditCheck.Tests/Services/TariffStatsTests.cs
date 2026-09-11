using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class TariffStatsTests
{
    [Fact]
    public async Task UsageRepository_SaveAndGetForDay_PersistsIsPeakAndStartTimeIso()
    {
        // Arrange
        var tempDb = Path.Combine(Path.GetTempPath(), $"test_tariff_{Guid.NewGuid():N}.db");
        try
        {
            var db = new AppDbContext(tempDb);
            await db.InitializeAsync();
            var repo = new UsageRepository(db);

            var items = new List<UsageDetailSnapshot>
            {
                new()
                {
                    Year = 2026,
                    Month = 8,
                    UtcDate = "2026-08-17",
                    StartTimeIso = "2026-08-17T02:00:00Z",
                    IsPeak = true,
                    Model = "deepseek-v4-pro",
                    ApiKeyName = "Key1",
                    ApiKeyMasked = "sk-1",
                    Type = "input_cache_hit_tokens",
                    Price = 0.000000003625,
                    Amount = 100000
                },
                new()
                {
                    Year = 2026,
                    Month = 8,
                    UtcDate = "2026-08-17",
                    StartTimeIso = "2026-08-17T15:00:00+02:00",
                    IsPeak = false,
                    Model = "deepseek-v4-flash",
                    ApiKeyName = "Key1",
                    ApiKeyMasked = "sk-1",
                    Type = "input_cache_hit_tokens",
                    Price = 0.000000007,
                    Amount = 200000
                },
                new()
                {
                    Year = 2026,
                    Month = 8,
                    UtcDate = "2026-08-18",
                    StartTimeIso = "2026-08-18T10:00:00Z",
                    IsPeak = false,
                    Model = "deepseek-v4-flash",
                    ApiKeyName = "Key1",
                    ApiKeyMasked = "sk-1",
                    Type = "input_cache_hit_tokens",
                    Price = 0.000000007,
                    Amount = 50000
                }
            };

            // Act
            await repo.SaveUsageDetailsAsync(2026, 8, items);
            var day17 = await repo.GetUsageDetailsForDayAsync(2026, 8, "2026-08-17");

            // Assert
            Assert.Equal(2, day17.Count);
            Assert.True(day17[0].IsPeak);
            Assert.Equal("2026-08-17T02:00:00Z", day17[0].StartTimeIso);

            Assert.False(day17[1].IsPeak);
            Assert.Equal("2026-08-17T15:00:00+02:00", day17[1].StartTimeIso);
        }
        finally
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            try { if (File.Exists(tempDb)) File.Delete(tempDb); } catch { }
        }
    }

    [Fact]
    public void TariffService_PeakWindows_EvaluatedCorrectly()
    {
        // 01:00 UTC -> Peak
        Assert.True(TariffService.IsPeak(new DateTime(2026, 8, 17, 1, 0, 0, DateTimeKind.Utc)));
        Assert.True(TariffService.IsPeak(new DateTime(2026, 8, 17, 3, 59, 59, DateTimeKind.Utc)));

        // 04:00 UTC -> Off-Peak
        Assert.False(TariffService.IsPeak(new DateTime(2026, 8, 17, 4, 0, 0, DateTimeKind.Utc)));
        Assert.False(TariffService.IsPeak(new DateTime(2026, 8, 17, 5, 59, 59, DateTimeKind.Utc)));

        // 06:00 UTC -> Peak
        Assert.True(TariffService.IsPeak(new DateTime(2026, 8, 17, 6, 0, 0, DateTimeKind.Utc)));
        Assert.True(TariffService.IsPeak(new DateTime(2026, 8, 17, 9, 59, 59, DateTimeKind.Utc)));

        // 10:00 UTC -> Off-Peak
        Assert.False(TariffService.IsPeak(new DateTime(2026, 8, 17, 10, 0, 0, DateTimeKind.Utc)));
        Assert.False(TariffService.IsPeak(new DateTime(2026, 8, 17, 23, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void DetermineIsPeak_ByPrice_DetectsPeakAndOffPeakAccurately()
    {
        // V4-Flash (starší ceny) Input Miss: Off-Peak = 0.00000022 ($0.22/M), Peak = 0.00000044 ($0.44/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-flash", "input_cache_miss_tokens", 0.00000044, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-flash", "input_cache_miss_tokens", 0.00000022, ""));

        // V4-Flash (starší ceny) Output: Off-Peak = 0.00000066 ($0.66/M), Peak = 0.00000132 ($1.32/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-flash", "output_tokens", 0.00000132, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-flash", "output_tokens", 0.00000066, ""));

        // V4.1-Flash (deepseek-flash nové ceny od 10.9.2026):
        // Input Miss: Off-Peak = 0.00000015 ($0.15/M), Peak = 0.00000030 ($0.30/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-flash", "input_cache_miss_tokens", 0.00000030, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-flash", "input_cache_miss_tokens", 0.00000015, ""));

        // Input Hit: Off-Peak = 0.000000003 ($0.003/M), Peak = 0.000000006 ($0.006/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-flash", "input_cache_hit_tokens", 0.000000006, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-flash", "input_cache_hit_tokens", 0.000000003, ""));

        // Output: Off-Peak = 0.00000060 ($0.60/M), Peak = 0.00000120 ($1.20/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-flash", "output_tokens", 0.00000120, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-flash", "output_tokens", 0.00000060, ""));

        // Pro Input Miss: Off-Peak = 0.00000066 ($0.66/M), Peak = 0.00000132 ($1.32/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-pro", "input_cache_miss_tokens", 0.00000132, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-pro", "input_cache_miss_tokens", 0.00000066, ""));

        // Pro Output: Off-Peak = 0.00000198 ($1.98/M), Peak = 0.00000396 ($3.96/M)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-pro", "output_tokens", 0.00000396, ""));
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-pro", "output_tokens", 0.00000198, ""));
    }

    [Fact]
    public void DetermineIsPeak_ByTimestamp_EvaluatesUtcWindowsCorrectly()
    {
        // 08:20 CEST (+02:00) = 06:20 UTC -> Ve špičce (Peak)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-flash", "request_count", null, "2026-08-18T08:20:00+02:00"));

        // 06:30 UTC (Z) -> Ve špičce (Peak)
        Assert.True(TariffService.DetermineIsPeak("deepseek-v4-flash", "request_count", null, "2026-08-18T06:30:00Z"));

        // 15:00 CEST (+02:00) = 13:00 UTC -> Mimo špičku (Off-Peak)
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-flash", "request_count", null, "2026-08-18T15:00:00+02:00"));

        // Záznam pouze s datem bez času -> Default na False (nepřiřazuje falešně špičku)
        Assert.False(TariffService.DetermineIsPeak("deepseek-v4-flash", "request_count", null, "2026-08-18"));
    }
}
