using Microsoft.Data.Sqlite;
using Dapper;

namespace DeepSeekCreditCheck.Core.Data;

public class AppDbContext
{
    private readonly string _connectionString;

    public AppDbContext(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public async Task InitializeAsync()
    {
        using var connection = CreateConnection();
        connection.Open();

        var sql = @"CREATE TABLE IF NOT EXISTS BalanceSnapshots (
            SnapshotId      INTEGER PRIMARY KEY AUTOINCREMENT,
            Timestamp       TEXT    NOT NULL,
            Currency        TEXT    NOT NULL DEFAULT 'USD',
            TotalBalance    TEXT    NOT NULL DEFAULT '0.00'
        );

        CREATE TABLE IF NOT EXISTS AppSettings (
            Key   TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS MonthlyUsageDetails (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            Year            INTEGER NOT NULL,
            Month           INTEGER NOT NULL,
            UtcDate         TEXT    NOT NULL,
            StartTimeIso    TEXT,
            IsPeak          INTEGER NOT NULL DEFAULT 0,
            Model           TEXT    NOT NULL,
            ApiKeyName      TEXT    NOT NULL,
            ApiKeyMasked    TEXT    NOT NULL,
            Type            TEXT    NOT NULL,
            Price           REAL,
            Amount          INTEGER NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_balance_timestamp ON BalanceSnapshots(Timestamp);
        CREATE INDEX IF NOT EXISTS idx_monthly_usage_ym ON MonthlyUsageDetails(Year, Month);
        CREATE INDEX IF NOT EXISTS idx_monthly_usage_ym_date ON MonthlyUsageDetails(Year, Month, UtcDate);";

        await connection.ExecuteAsync(sql);

        // Bezpečná migrace pro existující databáze
        try { await connection.ExecuteAsync("ALTER TABLE MonthlyUsageDetails ADD COLUMN StartTimeIso TEXT;"); } catch { }
        try { await connection.ExecuteAsync("ALTER TABLE MonthlyUsageDetails ADD COLUMN IsPeak INTEGER NOT NULL DEFAULT 0;"); } catch { }
    }
}
