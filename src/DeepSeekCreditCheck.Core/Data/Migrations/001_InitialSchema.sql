-- src/DeepSeekCreditCheck.Core/Data/Migrations/001_InitialSchema.sql
CREATE TABLE IF NOT EXISTS BalanceSnapshots (
    SnapshotId      INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp       TEXT    NOT NULL,
    Currency        TEXT    NOT NULL DEFAULT 'USD',
    TotalBalance    TEXT    NOT NULL DEFAULT '0.00'
);

CREATE TABLE IF NOT EXISTS AppSettings (
    Key   TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_balance_timestamp ON BalanceSnapshots(Timestamp);
