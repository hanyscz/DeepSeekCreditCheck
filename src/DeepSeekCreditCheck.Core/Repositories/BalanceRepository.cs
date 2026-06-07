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
            @"INSERT INTO BalanceSnapshots (Timestamp, IsAvailable, Currency, TotalBalance, GrantedBalance, ToppedUpBalance)
              VALUES (@Timestamp, @IsAvailable, @Currency, @TotalBalance, @GrantedBalance, @ToppedUpBalance)",
            new
            {
                Timestamp = snapshot.Timestamp.ToString("O"),
                IsAvailable = snapshot.IsAvailable ? 1 : 0,
                snapshot.Currency,
                snapshot.TotalBalance,
                snapshot.GrantedBalance,
                snapshot.ToppedUpBalance
            });
    }

    public async Task<BalanceSnapshot?> GetLatestAsync()
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        return await conn.QueryFirstOrDefaultAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT 1");
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var results = await conn.QueryAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots WHERE Timestamp >= @since AND Timestamp <= @until ORDER BY Timestamp",
            new { since = since.ToString("O"), until = until.ToString("O") });
        return results.AsList();
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var results = await conn.QueryAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT @limit",
            new { limit });
        return results.AsList();
    }
}
