-- src/DeepSeekCreditCheck.Core/Data/Migrations/001_InitialSchema.sql
CREATE TABLE IF NOT EXISTS BalanceSnapshots (
    SnapshotId      INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp       TEXT    NOT NULL,
    IsAvailable     INTEGER NOT NULL DEFAULT 1,
    Currency        TEXT    NOT NULL DEFAULT 'USD',
    TotalBalance    TEXT    NOT NULL DEFAULT '0.00',
    GrantedBalance  TEXT    NOT NULL DEFAULT '0.00',
    ToppedUpBalance TEXT    NOT NULL DEFAULT '0.00'
);

CREATE TABLE IF NOT EXISTS UsageRecords (
    RecordId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp    TEXT    NOT NULL,
    PeriodStart  TEXT,
    PeriodEnd    TEXT,
    TotalTokens  INTEGER NOT NULL DEFAULT 0,
    InputTokens  INTEGER NOT NULL DEFAULT 0,
    OutputTokens INTEGER NOT NULL DEFAULT 0,
    CachedTokens INTEGER
);

CREATE TABLE IF NOT EXISTS AppSettings (
    Key   TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_balance_timestamp ON BalanceSnapshots(Timestamp);
CREATE INDEX IF NOT EXISTS idx_usage_timestamp ON UsageRecords(Timestamp);
