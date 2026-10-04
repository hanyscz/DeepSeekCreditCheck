using Dapper;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public class BalanceRepository : IBalanceRepository
{
    private readonly AppDbContext _db;

    public BalanceRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(BalanceSnapshot snapshot)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            @"INSERT INTO BalanceSnapshots (Timestamp, Currency, TotalBalance)
              VALUES (@Timestamp, @Currency, @TotalBalance)",
            new
            {
                Timestamp = snapshot.Timestamp.ToString("O"),
                snapshot.Currency,
                snapshot.TotalBalance
            });
    }

    public async Task<BalanceSnapshot?> GetLatestAsync(string? currency = "USD")
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        if (string.IsNullOrEmpty(currency))
        {
            return await conn.QueryFirstOrDefaultAsync<BalanceSnapshot>(
                "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT 1");
        }
        return await conn.QueryFirstOrDefaultAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots WHERE UPPER(Currency) = UPPER(@currency) ORDER BY Timestamp DESC LIMIT 1",
            new { currency });
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until, string? currency = "USD")
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        if (string.IsNullOrEmpty(currency))
        {
            var results = await conn.QueryAsync<BalanceSnapshot>(
                "SELECT * FROM BalanceSnapshots WHERE Timestamp >= @since AND Timestamp <= @until ORDER BY Timestamp",
                new { since = since.ToString("O"), until = until.ToString("O") });
            return results.AsList();
        }
        else
        {
            var results = await conn.QueryAsync<BalanceSnapshot>(
                "SELECT * FROM BalanceSnapshots WHERE Timestamp >= @since AND Timestamp <= @until AND UPPER(Currency) = UPPER(@currency) ORDER BY Timestamp",
                new { since = since.ToString("O"), until = until.ToString("O"), currency });
            return results.AsList();
        }
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100, string? currency = "USD")
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        if (string.IsNullOrEmpty(currency))
        {
            var results = await conn.QueryAsync<BalanceSnapshot>(
                "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT @limit",
                new { limit });
            return results.AsList();
        }
        else
        {
            var results = await conn.QueryAsync<BalanceSnapshot>(
                "SELECT * FROM BalanceSnapshots WHERE UPPER(Currency) = UPPER(@currency) ORDER BY Timestamp DESC LIMIT @limit",
                new { limit, currency });
            return results.AsList();
        }
    }

    public async Task DeleteAsync(IEnumerable<int> ids)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var list = ids.ToList();
        if (list.Count == 0) return;
        await conn.ExecuteAsync(
            "DELETE FROM BalanceSnapshots WHERE SnapshotId IN @ids",
            new { ids = list });
    }

    public async Task DeleteAllAsync()
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        await conn.ExecuteAsync("DELETE FROM BalanceSnapshots");
    }
}