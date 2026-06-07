using Dapper;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _db;

    public UsageRepository(AppDbContext db) => _db = db;

    public async Task SaveAsync(UsageRecord record)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            @"INSERT INTO UsageRecords (Timestamp, PeriodStart, PeriodEnd, TotalTokens, InputTokens, OutputTokens, CachedTokens)
              VALUES (@Timestamp, @PeriodStart, @PeriodEnd, @TotalTokens, @InputTokens, @OutputTokens, @CachedTokens)",
            new
            {
                Timestamp = record.Timestamp.ToString("O"),
                PeriodStart = record.PeriodStart?.ToString("O"),
                PeriodEnd = record.PeriodEnd?.ToString("O"),
                record.TotalTokens,
                record.InputTokens,
                record.OutputTokens,
                record.CachedTokens
            });
    }

    public async Task<IReadOnlyList<UsageRecord>> GetHistoryAsync(DateTime since, DateTime until)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var results = await conn.QueryAsync<UsageRecord>(
            "SELECT * FROM UsageRecords WHERE Timestamp >= @since AND Timestamp <= @until ORDER BY Timestamp",
            new { since = since.ToString("O"), until = until.ToString("O") });
        return results.AsList();
    }

    public async Task<UsageRecord?> GetLatestAsync()
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        return await conn.QueryFirstOrDefaultAsync<UsageRecord>(
            "SELECT * FROM UsageRecords ORDER BY Timestamp DESC LIMIT 1");
    }
}
