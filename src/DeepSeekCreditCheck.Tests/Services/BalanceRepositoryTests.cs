using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class BalanceRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly AppDbContext _db;
    private readonly BalanceRepository _repo;

    public BalanceRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"test_balance_db_{Guid.NewGuid()}.db");
        _db = new AppDbContext(_dbPath);
        _db.InitializeAsync().GetAwaiter().GetResult();
        _repo = new BalanceRepository(_db);
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
    public async Task GetLatestAsync_Default_ReturnsLatestUsd()
    {
        var t1 = DateTime.UtcNow.AddMinutes(-10);
        var t2 = DateTime.UtcNow.AddMinutes(-5);

        await _repo.SaveAsync(new BalanceSnapshot
        {
            Timestamp = t1,
            Currency = "USD",
            TotalBalance = "17.13"
        });

        await _repo.SaveAsync(new BalanceSnapshot
        {
            Timestamp = t2,
            Currency = "CNY",
            TotalBalance = "6.00"
        });

        var latestUsd = await _repo.GetLatestAsync();
        Assert.NotNull(latestUsd);
        Assert.Equal("USD", latestUsd!.Currency);
        Assert.Equal("17.13", latestUsd.TotalBalance);

        var latestCny = await _repo.GetLatestAsync("CNY");
        Assert.NotNull(latestCny);
        Assert.Equal("CNY", latestCny!.Currency);
        Assert.Equal("6.00", latestCny.TotalBalance);
    }

    [Fact]
    public async Task GetAllAsync_FilteredByUsd_ReturnsOnlyUsd()
    {
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-20), Currency = "USD", TotalBalance = "20.00" });
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-15), Currency = "CNY", TotalBalance = "6.00" });
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = DateTime.UtcNow.AddMinutes(-10), Currency = "USD", TotalBalance = "19.00" });

        var usdOnly = await _repo.GetAllAsync(limit: 100, currency: "USD");
        Assert.Equal(2, usdOnly.Count);
        Assert.All(usdOnly, s => Assert.Equal("USD", s.Currency));

        var all = await _repo.GetAllAsync(limit: 100, currency: null);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task GetHistoryAsync_FilteredByUsd_ReturnsOnlyUsd()
    {
        var now = DateTime.UtcNow;
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = now.AddHours(-3), Currency = "USD", TotalBalance = "10.00" });
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = now.AddHours(-2), Currency = "CNY", TotalBalance = "5.00" });
        await _repo.SaveAsync(new BalanceSnapshot { Timestamp = now.AddHours(-1), Currency = "USD", TotalBalance = "9.00" });

        var usdHistory = await _repo.GetHistoryAsync(now.AddHours(-4), now, currency: "USD");
        Assert.Equal(2, usdHistory.Count);
        Assert.All(usdHistory, s => Assert.Equal("USD", s.Currency));
    }
}
