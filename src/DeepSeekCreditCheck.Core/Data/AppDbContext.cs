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
            IsAvailable     INTEGER NOT NULL DEFAULT 1,
            Currency        TEXT    NOT NULL DEFAULT 'USD',
            TotalBalance    TEXT    NOT NULL DEFAULT '0.00'
        );

        CREATE TABLE IF NOT EXISTS AppSettings (
            Key   TEXT PRIMARY KEY,
            Value TEXT NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_balance_timestamp ON BalanceSnapshots(Timestamp);";

        await connection.ExecuteAsync(sql);
    }
}
