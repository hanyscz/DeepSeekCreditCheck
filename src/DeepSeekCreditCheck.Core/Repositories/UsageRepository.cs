using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public class UsageRepository : IUsageRepository
{
    private readonly AppDbContext _db;

    public UsageRepository(AppDbContext db) => _db = db;

    public async Task SaveUsageDetailsAsync(int year, int month, List<UsageDetailSnapshot> details)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        using var transaction = conn.BeginTransaction();
        try
        {
            // 1. Smazat staré záznamy pro daný měsíc
            await conn.ExecuteAsync(
                "DELETE FROM MonthlyUsageDetails WHERE Year = @year AND Month = @month",
                new { year, month },
                transaction);

            // 2. Vložit nové záznamy
            if (details != null && details.Count > 0)
            {
                await conn.ExecuteAsync(
                    @"INSERT INTO MonthlyUsageDetails (Year, Month, UtcDate, Model, ApiKeyName, ApiKeyMasked, Type, Price, Amount)
                      VALUES (@Year, @Month, @UtcDate, @Model, @ApiKeyName, @ApiKeyMasked, @Type, @Price, @Amount)",
                    details,
                    transaction);
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<IReadOnlyList<UsageDetailSnapshot>> GetUsageDetailsAsync(int year, int month)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        var results = await conn.QueryAsync<UsageDetailSnapshot>(
            "SELECT * FROM MonthlyUsageDetails WHERE Year = @year AND Month = @month ORDER BY UtcDate, ApiKeyName, Model",
            new { year, month });
        return results.AsList();
    }

    public async Task<IReadOnlyList<MonthlyTotal>> GetMonthlyTotalsAsync()
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        
        var sql = @"
            SELECT 
                Year, 
                Month, 
                SUM(CASE WHEN Type = 'request_count' THEN 0 ELSE Price * Amount END) AS TotalCost,
                SUM(CASE WHEN Type = 'request_count' THEN 0 ELSE Amount END) AS TotalTokens,
                SUM(CASE WHEN Type = 'request_count' THEN Amount ELSE 0 END) AS RequestCount,
                SUM(CASE WHEN Type = 'input_cache_hit_tokens' THEN Amount ELSE 0 END) AS CacheHitTokens,
                SUM(CASE WHEN Type = 'input_cache_miss_tokens' THEN Amount ELSE 0 END) AS CacheMissTokens,
                SUM(CASE WHEN Type = 'output_tokens' THEN Amount ELSE 0 END) AS OutputTokens
            FROM MonthlyUsageDetails
            GROUP BY Year, Month
            ORDER BY Year DESC, Month DESC";
            
        var results = await conn.QueryAsync<MonthlyTotal>(sql);
        return results.AsList();
    }

    public async Task DeleteUsageDetailsAsync(int year, int month)
    {
        using var conn = _db.CreateConnection();
        conn.Open();
        await conn.ExecuteAsync(
            "DELETE FROM MonthlyUsageDetails WHERE Year = @year AND Month = @month",
            new { year, month });
    }
}
