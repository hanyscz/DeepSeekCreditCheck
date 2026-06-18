using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class UsageRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _db;
    private readonly UsageRepository _repo;

    public UsageRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_db_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_dbPath);
        _db.InitializeAsync().GetAwaiter().GetResult();
        _repo = new UsageRepository(_db);
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath))
        {
            try
            {
                File.Delete(_dbPath);
            }
            catch { }
        }
    }

    [Fact]
    public async Task SaveUsageDetailsAsync_SaveAndRetrieve_ReturnsSavedDetails()
    {
        // Arrange
        var details = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                UtcDate = "2026-06-01",
                Model = "deepseek-v4-pro",
                ApiKeyName = "VS Code Copilot",
                ApiKeyMasked = "sk-key1",
                Type = "input_cache_hit_tokens",
                Price = 0.000000003625,
                Amount = 1000
            },
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                UtcDate = "2026-06-01",
                Model = "deepseek-v4-pro",
                ApiKeyName = "VS Code Copilot",
                ApiKeyMasked = "sk-key1",
                Type = "output_tokens",
                Price = 0.00000087,
                Amount = 500
            }
        };

        // Act
        await _repo.SaveUsageDetailsAsync(2026, 6, details);
        var retrieved = await _repo.GetUsageDetailsAsync(2026, 6);

        // Assert
        Assert.Equal(2, retrieved.Count);
        Assert.Equal("VS Code Copilot", retrieved[0].ApiKeyName);
        Assert.Equal("input_cache_hit_tokens", retrieved[0].Type);
        Assert.Equal("output_tokens", retrieved[1].Type);
    }

    [Fact]
    public async Task SaveUsageDetailsAsync_OverwriteSameMonth_ReplacesOldDetails()
    {
        // Arrange
        var initialDetails = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                UtcDate = "2026-06-01",
                Model = "deepseek-v4-pro",
                ApiKeyName = "Old Key",
                Type = "request_count",
                Amount = 10
            }
        };

        var newDetails = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                UtcDate = "2026-06-02",
                Model = "deepseek-v4-flash",
                ApiKeyName = "New Key",
                Type = "request_count",
                Amount = 20
            }
        };

        // Act
        await _repo.SaveUsageDetailsAsync(2026, 6, initialDetails);
        await _repo.SaveUsageDetailsAsync(2026, 6, newDetails);
        var retrieved = await _repo.GetUsageDetailsAsync(2026, 6);

        // Assert
        Assert.Single(retrieved);
        Assert.Equal("New Key", retrieved[0].ApiKeyName);
        Assert.Equal("2026-06-02", retrieved[0].UtcDate);
        Assert.Equal("deepseek-v4-flash", retrieved[0].Model);
    }

    [Fact]
    public async Task GetMonthlyTotalsAsync_ReturnsCorrectTotalsAndTokens()
    {
        // Arrange
        var detailsJune = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                Type = "input_cache_hit_tokens",
                Price = 0.01,
                Amount = 100
            },
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                Type = "request_count", // request_count has no price and shouldn't count as tokens
                Price = null,
                Amount = 5
            }
        };

        var detailsMay = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 5,
                Type = "output_tokens",
                Price = 0.02,
                Amount = 50
            }
        };

        await _repo.SaveUsageDetailsAsync(2026, 6, detailsJune);
        await _repo.SaveUsageDetailsAsync(2026, 5, detailsMay);

        // Act
        var totals = await _repo.GetMonthlyTotalsAsync();

        // Assert
        Assert.Equal(2, totals.Count);
        
        var juneTotal = totals[0]; // Ordered by Year DESC, Month DESC
        Assert.Equal(2026, juneTotal.Year);
        Assert.Equal(6, juneTotal.Month);
        Assert.Equal(1.0, juneTotal.TotalCost); // 100 * 0.01
        Assert.Equal(100, juneTotal.TotalTokens);

        var mayTotal = totals[1];
        Assert.Equal(2026, mayTotal.Year);
        Assert.Equal(5, mayTotal.Month);
        Assert.Equal(1.0, mayTotal.TotalCost); // 50 * 0.02
        Assert.Equal(50, mayTotal.TotalTokens);
    }

    [Fact]
    public async Task DeleteUsageDetailsAsync_RemovesMonthDetails()
    {
        // Arrange
        var details = new List<UsageDetailSnapshot>
        {
            new UsageDetailSnapshot
            {
                Year = 2026,
                Month = 6,
                UtcDate = "2026-06-01",
                Model = "deepseek-v4-pro",
                ApiKeyName = "Key",
                Type = "request_count",
                Amount = 1
            }
        };
        await _repo.SaveUsageDetailsAsync(2026, 6, details);

        // Act
        await _repo.DeleteUsageDetailsAsync(2026, 6);
        var retrieved = await _repo.GetUsageDetailsAsync(2026, 6);

        // Assert
        Assert.Empty(retrieved);
    }
}
