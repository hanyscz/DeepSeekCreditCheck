# DeepSeek Credit Checker — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Vybudovat WPF tray aplikaci pro monitorování DeepSeek API kreditu s predikcí spotřeby.

**Architecture:** .NET 8 solution se 3 projekty — Core (business logika, SQLite přes Dapper, API klient), UI (WPF, Hardcodet tray, OxyPlot grafy), Tests (xUnit). Periodický polling volá DeepSeek API, data se ukládají do lokální SQLite, tray ikona zobrazuje aktuální stav.

**Tech Stack:** .NET 8, WPF, SQLite + Dapper, Hardcodet.NotifyIcon.Wpf, OxyPlot.Wpf, xUnit, Microsoft.Extensions.DependencyInjection

---

## File Structure Map

```
DeepSeekCreditCheck/
├── DeepSeekCreditCheck.sln
├── src/
│   ├── DeepSeekCreditCheck.Core/
│   │   ├── DeepSeekCreditCheck.Core.csproj
│   │   ├── Models/
│   │   │   ├── BalanceSnapshot.cs
│   │   │   └── UsageRecord.cs
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs
│   │   │   └── Migrations/
│   │   │       └── 001_InitialSchema.sql
│   │   ├── Repositories/
│   │   │   ├── IBalanceRepository.cs
│   │   │   ├── BalanceRepository.cs
│   │   │   ├── IUsageRepository.cs
│   │   │   └── UsageRepository.cs
│   │   ├── Services/
│   │   │   ├── IAppSettingsService.cs
│   │   │   ├── AppSettingsService.cs
│   │   │   ├── IDeepSeekApiClient.cs
│   │   │   ├── DeepSeekApiClient.cs
│   │   │   ├── IPollingService.cs
│   │   │   ├── PollingService.cs
│   │   │   ├── PredictionEngine.cs
│   │   │   └── AlertService.cs
│   │   └── Configuration/
│   │       └── DataProtection.cs
│   ├── DeepSeekCreditCheck.UI/
│   │   ├── DeepSeekCreditCheck.UI.csproj
│   │   ├── App.xaml / App.xaml.cs
│   │   ├── MainWindow.xaml / MainWindow.xaml.cs
│   │   ├── Windows/
│   │   │   ├── DashboardWindow.xaml / .cs
│   │   │   └── SettingsWindow.xaml / .cs
│   │   ├── ViewModels/
│   │   │   ├── BaseViewModel.cs
│   │   │   ├── DashboardViewModel.cs
│   │   │   └── SettingsViewModel.cs
│   │   ├── Services/
│   │   │   └── TrayIconService.cs
│   │   ├── Converters/
│   │   │   └── BoolToColorConverter.cs
│   │   └── Resources/
│   │       └── app.ico
│   └── DeepSeekCreditCheck.Tests/
│       ├── DeepSeekCreditCheck.Tests.csproj
│       ├── Services/
│       │   ├── PredictionEngineTests.cs
│       │   ├── AlertServiceTests.cs
│       │   └── DeepSeekApiClientTests.cs
│       └── Repositories/
│           └── BalanceRepositoryTests.cs
```

---

### Task 1: Vytvořit solution a projekty

**Files:**
- Create: `DeepSeekCreditCheck.sln`
- Create: `src/DeepSeekCreditCheck.Core/DeepSeekCreditCheck.Core.csproj`
- Create: `src/DeepSeekCreditCheck.UI/DeepSeekCreditCheck.UI.csproj`
- Create: `src/DeepSeekCreditCheck.Tests/DeepSeekCreditCheck.Tests.csproj`

- [ ] **Step 1: Vytvořit solution**

```bash
cd C:\Users\Hanys\source\repos\DeepSeekCreditCheck
dotnet new sln -n DeepSeekCreditCheck
```

- [ ] **Step 2: Vytvořit Core class library**

```bash
mkdir -p src/DeepSeekCreditCheck.Core
dotnet new classlib -n DeepSeekCreditCheck.Core -o src/DeepSeekCreditCheck.Core --framework net8.0
```

- [ ] **Step 3: Vytvořit WPF projekt a Tests projekt**

```bash
dotnet new wpf -n DeepSeekCreditCheck.UI -o src/DeepSeekCreditCheck.UI --framework net8.0
dotnet new xunit -n DeepSeekCreditCheck.Tests -o src/DeepSeekCreditCheck.Tests --framework net8.0
```

- [ ] **Step 4: Zaregistrovat projekty do solution**

```bash
dotnet sln add src/DeepSeekCreditCheck.Core/DeepSeekCreditCheck.Core.csproj
dotnet sln add src/DeepSeekCreditCheck.UI/DeepSeekCreditCheck.UI.csproj
dotnet sln add src/DeepSeekCreditCheck.Tests/DeepSeekCreditCheck.Tests.csproj
```

- [ ] **Step 5: Přidat NuGet balíčky do Core projektu**

```bash
cd src/DeepSeekCreditCheck.Core
dotnet add package Microsoft.Data.Sqlite --version 8.0.*
dotnet add package Dapper --version 2.*
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.*
dotnet add package System.Security.Cryptography.ProtectedData --version 8.0.*
```

- [ ] **Step 6: Přidat NuGet balíčky do UI projektu**

```bash
cd ../DeepSeekCreditCheck.UI
dotnet add package Hardcodet.NotifyIcon.Wpf --version 2.*
dotnet add package OxyPlot.Wpf --version 2.*
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.*
dotnet add reference ../DeepSeekCreditCheck.Core/DeepSeekCreditCheck.Core.csproj
```

- [ ] **Step 7: Přidat NuGet balíčky a referenci do Tests projektu**

```bash
cd ../DeepSeekCreditCheck.Tests
dotnet add package Moq --version 4.*
dotnet add package FluentAssertions --version 6.*
dotnet add reference ../DeepSeekCreditCheck.Core/DeepSeekCreditCheck.Core.csproj
```

- [ ] **Step 8: Ověřit build**

```bash
cd ../..
dotnet build
```

Expected: Build succeeded. 0 Error(s)

- [ ] **Step 9: Commit**

```bash
git add .
git commit -m "chore: create solution structure with Core, UI, Tests projects"
```

---

### Task 2: Core modely

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Models/BalanceSnapshot.cs`
- Create: `src/DeepSeekCreditCheck.Core/Models/UsageRecord.cs`

- [ ] **Step 1: Napsat BalanceSnapshot model**

```csharp
// src/DeepSeekCreditCheck.Core/Models/BalanceSnapshot.cs
namespace DeepSeekCreditCheck.Core.Models;

public class BalanceSnapshot
{
    public int SnapshotId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsAvailable { get; set; }
    public string Currency { get; set; } = "USD";
    public string TotalBalance { get; set; } = "0.00";
    public string GrantedBalance { get; set; } = "0.00";
    public string ToppedUpBalance { get; set; } = "0.00";

    // Helper properta pro výpočty
    public decimal TotalBalanceDecimal => decimal.TryParse(TotalBalance, out var v) ? v : 0;
    public decimal GrantedBalanceDecimal => decimal.TryParse(GrantedBalance, out var v) ? v : 0;
    public decimal ToppedUpBalanceDecimal => decimal.TryParse(ToppedUpBalance, out var v) ? v : 0;
}
```

- [ ] **Step 2: Napsat UsageRecord model**

```csharp
// src/DeepSeekCreditCheck.Core/Models/UsageRecord.cs
namespace DeepSeekCreditCheck.Core.Models;

public class UsageRecord
{
    public int RecordId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public long TotalTokens { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long? CachedTokens { get; set; }
}
```

- [ ] **Step 3: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Models/
git commit -m "feat: add BalanceSnapshot and UsageRecord models"
```

---

### Task 3: Databázová vrstva

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Data/AppDbContext.cs`
- Create: `src/DeepSeekCreditCheck.Core/Data/Migrations/001_InitialSchema.sql`
- Create: `src/DeepSeekCreditCheck.Core/Repositories/IBalanceRepository.cs`
- Create: `src/DeepSeekCreditCheck.Core/Repositories/BalanceRepository.cs`
- Create: `src/DeepSeekCreditCheck.Core/Repositories/IUsageRepository.cs`
- Create: `src/DeepSeekCreditCheck.Core/Repositories/UsageRepository.cs`

- [ ] **Step 1: SQL migrační skript**

```sql
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
```

- [ ] **Step 2: AppDbContext**

```csharp
// src/DeepSeekCreditCheck.Core/Data/AppDbContext.cs
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

        // Ensure tables
        var migrationFolder = Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "DeepSeekCreditCheck.Core", "Data", "Migrations");

        // Pro embedded resource přístup v produkci:
        // Použijeme inline, protože cesta se liší mezi dev/prod
        var sql = @"CREATE TABLE IF NOT EXISTS BalanceSnapshots (
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
        CREATE INDEX IF NOT EXISTS idx_usage_timestamp ON UsageRecords(Timestamp);";

        await connection.ExecuteAsync(sql);
    }
}
```

- [ ] **Step 3: Repository rozhraní**

```csharp
// src/DeepSeekCreditCheck.Core/Repositories/IBalanceRepository.cs
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IBalanceRepository
{
    Task SaveAsync(BalanceSnapshot snapshot);
    Task<BalanceSnapshot?> GetLatestAsync();
    Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until);
    Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100);
}
```

```csharp
// src/DeepSeekCreditCheck.Core/Repositories/IUsageRepository.cs
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Repositories;

public interface IUsageRepository
{
    Task SaveAsync(UsageRecord record);
    Task<IReadOnlyList<UsageRecord>> GetHistoryAsync(DateTime since, DateTime until);
    Task<UsageRecord?> GetLatestAsync();
}
```

- [ ] **Step 4: BalanceRepository implementace**

```csharp
// src/DeepSeekCreditCheck.Core/Repositories/BalanceRepository.cs
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
        return await conn.QueryFirstOrDefaultAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT 1");
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetHistoryAsync(DateTime since, DateTime until)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots WHERE Timestamp >= @since AND Timestamp <= @until ORDER BY Timestamp",
            new { since = since.ToString("O"), until = until.ToString("O") });
        return results.AsList();
    }

    public async Task<IReadOnlyList<BalanceSnapshot>> GetAllAsync(int limit = 100)
    {
        using var conn = _db.CreateConnection();
        var results = await conn.QueryAsync<BalanceSnapshot>(
            "SELECT * FROM BalanceSnapshots ORDER BY Timestamp DESC LIMIT @limit",
            new { limit });
        return results.AsList();
    }
}
```

- [ ] **Step 5: UsageRepository implementace**

```csharp
// src/DeepSeekCreditCheck.Core/Repositories/UsageRepository.cs
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
        var results = await conn.QueryAsync<UsageRecord>(
            "SELECT * FROM UsageRecords WHERE Timestamp >= @since AND Timestamp <= @until ORDER BY Timestamp",
            new { since = since.ToString("O"), until = until.ToString("O") });
        return results.AsList();
    }

    public async Task<UsageRecord?> GetLatestAsync()
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<UsageRecord>(
            "SELECT * FROM UsageRecords ORDER BY Timestamp DESC LIMIT 1");
    }
}
```

- [ ] **Step 6: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Data/ src/DeepSeekCreditCheck.Core/Repositories/
git commit -m "feat: add database layer with SQLite, Dapper repos"
```

---

### Task 4: Nastavení a šifrování API klíče

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Configuration/DataProtection.cs`
- Create: `src/DeepSeekCreditCheck.Core/Services/IAppSettingsService.cs`
- Create: `src/DeepSeekCreditCheck.Core/Services/AppSettingsService.cs`

- [ ] **Step 1: DataProtection helper**

```csharp
// src/DeepSeekCreditCheck.Core/Configuration/DataProtection.cs
using System.Security.Cryptography;

namespace DeepSeekCreditCheck.Core.Configuration;

public static class DataProtection
{
    public static string Protect(string plainText)
    {
        byte[] plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        byte[] protectedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    public static string Unprotect(string protectedText)
    {
        byte[] protectedBytes = Convert.FromBase64String(protectedText);
        byte[] plainBytes = ProtectedData.Unprotect(protectedBytes, null, DataProtectionScope.CurrentUser);
        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}
```

- [ ] **Step 2: IAppSettingsService rozhraní**

```csharp
// src/DeepSeekCreditCheck.Core/Services/IAppSettingsService.cs
namespace DeepSeekCreditCheck.Core.Services;

public interface IAppSettingsService
{
    Task<string?> GetAsync(string key);
    Task SetAsync(string key, string value);
    Task<string?> GetApiKeyAsync();
    Task SetApiKeyAsync(string apiKey);
    Task<string?> GetAlertThresholdAsync();
    Task SetAlertThresholdAsync(string threshold);
    Task<int> GetPollingIntervalMinutesAsync();
    Task SetPollingIntervalMinutesAsync(int minutes);
}
```

- [ ] **Step 3: AppSettingsService implementace**

```csharp
// src/DeepSeekCreditCheck.Core/Services/AppSettingsService.cs
using Dapper;
using DeepSeekCreditCheck.Core.Configuration;
using DeepSeekCreditCheck.Core.Data;

namespace DeepSeekCreditCheck.Core.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly AppDbContext _db;

    public AppSettingsService(AppDbContext db) => _db = db;

    public async Task<string?> GetAsync(string key)
    {
        using var conn = _db.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<string?>(
            "SELECT Value FROM AppSettings WHERE Key = @key", new { key });
    }

    public async Task SetAsync(string key, string value)
    {
        using var conn = _db.CreateConnection();
        await conn.ExecuteAsync(
            @"INSERT OR REPLACE INTO AppSettings (Key, Value) VALUES (@key, @value)",
            new { key, value });
    }

    public async Task<string?> GetApiKeyAsync()
    {
        var encrypted = await GetAsync("ApiKey");
        if (string.IsNullOrEmpty(encrypted)) return null;
        try { return DataProtection.Unprotect(encrypted); }
        catch { return null; }
    }

    public async Task SetApiKeyAsync(string apiKey)
    {
        var encrypted = DataProtection.Protect(apiKey);
        await SetAsync("ApiKey", encrypted);
    }

    public async Task<string?> GetAlertThresholdAsync()
        => await GetAsync("AlertThreshold") ?? "2.00";

    public async Task SetAlertThresholdAsync(string threshold)
        => await SetAsync("AlertThreshold", threshold);

    public async Task<int> GetPollingIntervalMinutesAsync()
    {
        var val = await GetAsync("PollingIntervalMin");
        return int.TryParse(val, out var i) ? i : 15;
    }

    public async Task SetPollingIntervalMinutesAsync(int minutes)
        => await SetAsync("PollingIntervalMin", minutes.ToString());
}
```

- [ ] **Step 4: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Configuration/ src/DeepSeekCreditCheck.Core/Services/
git commit -m "feat: add settings service with DPAPI encryption"
```

---

### Task 5: DeepSeek API Client

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Services/IDeepSeekApiClient.cs`
- Create: `src/DeepSeekCreditCheck.Core/Services/DeepSeekApiClient.cs`

- [ ] **Step 1: IDeepSeekApiClient rozhraní**

```csharp
// src/DeepSeekCreditCheck.Core/Services/IDeepSeekApiClient.cs
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public interface IDeepSeekApiClient
{
    Task<BalanceSnapshot> GetBalanceAsync(string apiKey);
    Task<UsageRecord?> GetUsageAsync(string apiKey, DateTime? since = null, DateTime? until = null);
}
```

- [ ] **Step 2: DeepSeekApiClient implementace**

```csharp
// src/DeepSeekCreditCheck.Core/Services/DeepSeekApiClient.cs
using System.Net.Http.Json;
using System.Text.Json;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class DeepSeekApiClient : IDeepSeekApiClient
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.deepseek.com";

    public DeepSeekApiClient(HttpClient http)
    {
        _http = http;
        _http.BaseAddress = new Uri(BaseUrl);
        _http.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<BalanceSnapshot> GetBalanceAsync(string apiKey)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/user/balance");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var isAvailable = json.GetProperty("is_available").GetBoolean();
        var infos = json.GetProperty("balance_infos");
        var info = infos[0];

        return new BalanceSnapshot
        {
            Timestamp = DateTime.UtcNow,
            IsAvailable = isAvailable,
            Currency = info.GetProperty("currency").GetString() ?? "USD",
            TotalBalance = info.GetProperty("total_balance").GetString() ?? "0.00",
            GrantedBalance = info.GetProperty("granted_balance").GetString() ?? "0.00",
            ToppedUpBalance = info.GetProperty("topped_up_balance").GetString() ?? "0.00"
        };
    }

    public async Task<UsageRecord?> GetUsageAsync(string apiKey, DateTime? since = null, DateTime? until = null)
    {
        var start = since?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.AddDays(-7).ToString("yyyy-MM-dd");
        var end = until?.ToString("yyyy-MM-dd") ?? DateTime.UtcNow.ToString("yyyy-MM-dd");

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/v1/usage?start_time={start}&end_time={end}");
        request.Headers.Add("Authorization", $"Bearer {apiKey}");

        var response = await _http.SendAsync(request);

        // Usage endpoint nemusí existovat — vracíme null, fallback výpočet
        if (!response.IsSuccessStatusCode) return null;

        try
        {
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var total = json.GetProperty("total_usage");

            return new UsageRecord
            {
                Timestamp = DateTime.UtcNow,
                PeriodStart = since ?? DateTime.UtcNow.AddDays(-7),
                PeriodEnd = until ?? DateTime.UtcNow,
                TotalTokens = total.GetProperty("total_tokens").GetInt64(),
                InputTokens = total.GetProperty("input_tokens").GetInt64(),
                OutputTokens = total.GetProperty("output_tokens").GetInt64(),
                CachedTokens = total.TryGetProperty("cached_tokens", out var ct)
                    ? ct.GetInt64() : null
            };
        }
        catch
        {
            return null; // Neznámý formát odpovědi
        }
    }
}
```

- [ ] **Step 3: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Services/IDeepSeekApiClient.cs src/DeepSeekCreditCheck.Core/Services/DeepSeekApiClient.cs
git commit -m "feat: add DeepSeek API client for balance and usage"
```

---

### Task 6: Predikční engine

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Services/PredictionEngine.cs`

- [ ] **Step 1: Napsat PredictionEngine**

```csharp
// src/DeepSeekCreditCheck.Core/Services/PredictionEngine.cs
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class PredictionEngine
{
    /// <summary>
    /// Spočítá předpokládaný počet dní, na který kredit vydrží.
    /// </summary>
    /// <param name="history">Historie balance snapshotů, seřazená vzestupně dle času.</param>
    /// <param name="currentBalance">Aktuální total balance v USD.</param>
    /// <returns>Predikce s počtem dní a denním průměrem.</returns>
    public PredictionResult Calculate(IReadOnlyList<BalanceSnapshot> history, decimal currentBalance)
    {
        if (history.Count < 2 || currentBalance <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        // Seřadit vzestupně
        var sorted = history.OrderBy(h => h.Timestamp).ToList();

        // Najít snapshoty s různými dny pro výpočet denní spotřeby
        var firstSnapshot = sorted.First();
        var lastSnapshot = sorted.Last();

        var totalSpend = firstSnapshot.TotalBalanceDecimal - lastSnapshot.TotalBalanceDecimal;
        var totalDays = (lastSnapshot.Timestamp - firstSnapshot.Timestamp).TotalDays;

        // Pokud data pokrývají méně než 1 hodinu, predikce není spolehlivá
        if (totalDays < 0.04) // ~1 hodina
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var avgDailySpend = totalSpend / (decimal)totalDays;

        if (avgDailySpend <= 0)
            return new PredictionResult { DaysRemaining = null, AvgDailySpend = 0, IsReliable = false };

        var daysRemaining = currentBalance / avgDailySpend;

        // Výpočet volatility pro pásmovou predikci
        var dailySpends = new List<decimal>();
        for (int i = 1; i < sorted.Count; i++)
        {
            var dayDiff = (sorted[i].Timestamp - sorted[i - 1].Timestamp).TotalDays;
            if (dayDiff > 0.001)
            {
                var spend = (sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal) / (decimal)dayDiff;
                if (spend >= 0) dailySpends.Add(spend);
            }
        }

        var isReliable = dailySpends.Count >= 3;
        decimal? rangeLow = null;
        decimal? rangeHigh = null;

        if (isReliable && dailySpends.Count > 1)
        {
            var mean = dailySpends.Average();
            var sumOfSquares = dailySpends.Sum(d => (d - mean) * (d - mean));
            var stdDev = (decimal)Math.Sqrt((double)(sumOfSquares / dailySpends.Count));

            if (stdDev > 0.3m * mean) // Vysoká volatilita → pásmo
            {
                rangeLow = currentBalance / (mean + stdDev);
                rangeHigh = currentBalance / Math.Max(mean - stdDev, 0.001m);
            }
        }

        return new PredictionResult
        {
            DaysRemaining = daysRemaining,
            RangeLow = rangeLow,
            RangeHigh = rangeHigh,
            AvgDailySpend = avgDailySpend,
            IsReliable = isReliable
        };
    }
}

public class PredictionResult
{
    public decimal? DaysRemaining { get; set; }
    public decimal? RangeLow { get; set; }
    public decimal? RangeHigh { get; set; }
    public decimal AvgDailySpend { get; set; }
    public bool IsReliable { get; set; }

    public string FormattedPrediction
    {
        get
        {
            if (!DaysRemaining.HasValue) return "—";
            if (RangeLow.HasValue && RangeHigh.HasValue)
                return $"~{RangeLow.Value:F0}-{RangeHigh.Value:F0} dní";
            if (DaysRemaining.Value > 365) return "> 1 rok";
            if (DaysRemaining.Value > 30) return $"~{DaysRemaining.Value / 30:F0} měsíců";
            return $"~{DaysRemaining.Value:F0} dní";
        }
    }

    public string FormattedDailySpend =>
        AvgDailySpend > 0 ? $"${AvgDailySpend:F2}/den" : "—";
}
```

- [ ] **Step 2: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Services/PredictionEngine.cs
git commit -m "feat: add prediction engine for credit duration"
```

---

### Task 7: Alert Service

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Services/AlertService.cs`

- [ ] **Step 1: Napsat AlertService**

```csharp
// src/DeepSeekCreditCheck.Core/Services/AlertService.cs
namespace DeepSeekCreditCheck.Core.Services;

public class AlertService
{
    private decimal _lastNotifiedBalance = decimal.MaxValue;
    private bool _wasBelowThreshold = false;

    public event EventHandler<AlertEventArgs>? AlertTriggered;

    /// <summary>
    /// Zkontroluje zůstatek proti prahu. Vyvolá AlertTriggered při poklesu pod práh.
    /// </summary>
    public void Check(decimal currentBalance, decimal threshold)
    {
        var isBelow = currentBalance < threshold;

        // Notifikujeme jen při přechodu z "nad prahem" do "pod prahem"
        // nebo když balance dál klesá pod prahem (každých ~10 % poklesu od poslední notifikace)
        if (isBelow)
        {
            if (!_wasBelowThreshold || currentBalance < _lastNotifiedBalance * 0.9m)
            {
                _lastNotifiedBalance = currentBalance;
                AlertTriggered?.Invoke(this, new AlertEventArgs
                {
                    CurrentBalance = currentBalance,
                    Threshold = threshold,
                    Message = $"⚠️ DeepSeek kredit klesl pod ${threshold:F2} — aktuálně ${currentBalance:F2}"
                });
            }
        }
        else
        {
            // Reset — balance je zpět nad prahem
            if (_wasBelowThreshold)
            {
                _lastNotifiedBalance = decimal.MaxValue;
            }
        }

        _wasBelowThreshold = isBelow;
    }
}

public class AlertEventArgs : EventArgs
{
    public decimal CurrentBalance { get; init; }
    public decimal Threshold { get; init; }
    public string Message { get; init; } = string.Empty;
}
```

- [ ] **Step 2: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Services/AlertService.cs
git commit -m "feat: add alert service with threshold notifications"
```

---

### Task 8: Polling Service

**Files:**
- Create: `src/DeepSeekCreditCheck.Core/Services/IPollingService.cs`
- Create: `src/DeepSeekCreditCheck.Core/Services/PollingService.cs`

- [ ] **Step 1: IPollingService rozhraní**

```csharp
// src/DeepSeekCreditCheck.Core/Services/IPollingService.cs
namespace DeepSeekCreditCheck.Core.Services;

public interface IPollingService
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    event EventHandler<PollResult>? PollCompleted;
}
```

- [ ] **Step 2: PollingService implementace**

```csharp
// src/DeepSeekCreditCheck.Core/Services/PollingService.cs
using DeepSeekCreditCheck.Core.Repositories;

namespace DeepSeekCreditCheck.Core.Services;

public class PollingService : IPollingService
{
    private readonly IDeepSeekApiClient _apiClient;
    private readonly IBalanceRepository _balanceRepo;
    private readonly IUsageRepository _usageRepo;
    private readonly IAppSettingsService _settings;
    private readonly PredictionEngine _predictionEngine;
    private readonly AlertService _alertService;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    public event EventHandler<PollResult>? PollCompleted;

    public PollingService(
        IDeepSeekApiClient apiClient,
        IBalanceRepository balanceRepo,
        IUsageRepository usageRepo,
        IAppSettingsService settings,
        PredictionEngine predictionEngine,
        AlertService alertService)
    {
        _apiClient = apiClient;
        _balanceRepo = balanceRepo;
        _usageRepo = usageRepo;
        _settings = settings;
        _predictionEngine = predictionEngine;
        _alertService = alertService;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var interval = TimeSpan.FromMinutes(await _settings.GetPollingIntervalMinutesAsync());

        // Okamžitě první poll
        await PollOnceAsync(ct);

        _timer = new PeriodicTimer(interval);
        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await PollOnceAsync(_cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        _timer?.Dispose();
        _timer = null;
        _cts?.Cancel();
        await Task.CompletedTask;
    }

    public async Task PollOnceAsync(CancellationToken ct)
    {
        var apiKey = await _settings.GetApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            // 1. Získat balance
            var snapshot = await _apiClient.GetBalanceAsync(apiKey);
            await _balanceRepo.SaveAsync(snapshot);

            // 2. Získat usage (volitelné, může selhat)
            var usage = await _apiClient.GetUsageAsync(apiKey);
            if (usage != null)
                await _usageRepo.SaveAsync(usage);

            // 3. Predikce
            var history = await _balanceRepo.GetAllAsync(limit: 500);
            var prediction = _predictionEngine.Calculate(history, snapshot.TotalBalanceDecimal);

            // 4. Alert
            var thresholdStr = await _settings.GetAlertThresholdAsync();
            var threshold = decimal.TryParse(thresholdStr, out var t) ? t : 2.00m;
            _alertService.Check(snapshot.TotalBalanceDecimal, threshold);

            // 5. Notifikovat UI
            PollCompleted?.Invoke(this, new PollResult
            {
                Snapshot = snapshot,
                Usage = usage,
                Prediction = prediction,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (HttpRequestException)
        {
            // API nedostupné — ignorovat, zkusí se znovu za interval
        }
        catch (TaskCanceledException) { }
    }
}

public class PollResult
{
    public Models.BalanceSnapshot? Snapshot { get; init; }
    public Models.UsageRecord? Usage { get; init; }
    public PredictionResult? Prediction { get; init; }
    public DateTime Timestamp { get; init; }
}
```

- [ ] **Step 2: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.Core
```

Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/DeepSeekCreditCheck.Core/Services/IPollingService.cs src/DeepSeekCreditCheck.Core/Services/PollingService.cs
git commit -m "feat: add polling service with timer orchestration"
```

---

### Task 9: WPF App Shell + Tray Icon

**Files:**
- Modify: `src/DeepSeekCreditCheck.UI/App.xaml`
- Modify: `src/DeepSeekCreditCheck.UI/App.xaml.cs`
- Modify: `src/DeepSeekCreditCheck.UI/MainWindow.xaml`
- Modify: `src/DeepSeekCreditCheck.UI/MainWindow.xaml.cs`
- Create: `src/DeepSeekCreditCheck.UI/Services/TrayIconService.cs`
- Create: `src/DeepSeekCreditCheck.UI/Resources/app.ico` (placeholder)

- [ ] **Step 1: App.xaml — odebrat StartupUri (tray app, žádné okno při startu)**

```xml
<!-- src/DeepSeekCreditCheck.UI/App.xaml -->
<Application x:Class="DeepSeekCreditCheck.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <!-- Merged dictionaries, styles později -->
    </Application.Resources>
</Application>
```

- [ ] **Step 2: App.xaml.cs — startup s DI**

```csharp
// src/DeepSeekCreditCheck.UI/App.xaml.cs
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using DeepSeekCreditCheck.UI.Services;
using DeepSeekCreditCheck.UI.ViewModels;
using DeepSeekCreditCheck.UI.Windows;

namespace DeepSeekCreditCheck.UI;

public partial class App : Application
{
    private IServiceProvider _services = null!;
    private TrayIconService _trayIcon = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekCreditCheck", "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        // DB
        var db = new AppDbContext(dbPath);
        await db.InitializeAsync();
        services.AddSingleton(db);

        // Repositories
        services.AddSingleton<IBalanceRepository, BalanceRepository>();
        services.AddSingleton<IUsageRepository, UsageRepository>();

        // Services
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IDeepSeekApiClient, DeepSeekApiClient>();
        services.AddSingleton<PredictionEngine>();
        services.AddSingleton<AlertService>();
        services.AddSingleton<IPollingService, PollingService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();

        _services = services.BuildServiceProvider();

        // Tray icon
        _trayIcon = new TrayIconService(_services);
        _trayIcon.Initialize();

        // Spustit polling
        var polling = _services.GetRequiredService<IPollingService>();
        var cts = new CancellationTokenSource();
        await polling.StartAsync(cts.Token);

        // Eventy
        polling.PollCompleted += (_, result) =>
        {
            Dispatcher.Invoke(() =>
            {
                _trayIcon.UpdateTooltip(result);
                _services.GetRequiredService<DashboardViewModel>().OnPollCompleted(result);
            });
        };

        var alertService = _services.GetRequiredService<AlertService>();
        alertService.AlertTriggered += (_, args) =>
        {
            Dispatcher.Invoke(() => _trayIcon.ShowNotification(args.Message));
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 3: MainWindow — skrytý**

```xml
<!-- src/DeepSeekCreditCheck.UI/MainWindow.xaml -->
<Window x:Class="DeepSeekCreditCheck.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="DeepSeek Credit Check"
        ShowInTaskbar="False"
        WindowState="Minimized"
        Visibility="Hidden"
        Width="0" Height="0">
</Window>
```

```csharp
// src/DeepSeekCreditCheck.UI/MainWindow.xaml.cs
using System.Windows;

namespace DeepSeekCreditCheck.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

- [ ] **Step 4: TrayIconService**

```csharp
// src/DeepSeekCreditCheck.UI/Services/TrayIconService.cs
using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using DeepSeekCreditCheck.Core.Services;
using DeepSeekCreditCheck.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSeekCreditCheck.UI.Services;

public class TrayIconService : IDisposable
{
    private readonly IServiceProvider _services;
    private TaskbarIcon? _notifyIcon;

    public TrayIconService(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize()
    {
        _notifyIcon = new TaskbarIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("No main module")),
            ToolTipText = "DeepSeek Credit Check",
            Visibility = Visibility.Visible
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var balanceItem = new System.Windows.Controls.MenuItem
        {
            Header = "💰 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(balanceItem);

        var toppedUpItem = new System.Windows.Controls.MenuItem
        {
            Header = "🔁 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(toppedUpItem);

        var predictionItem = new System.Windows.Controls.MenuItem
        {
            Header = "📊 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(predictionItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var dashboardItem = new System.Windows.Controls.MenuItem { Header = "📈 Dashboard" };
        dashboardItem.Click += (_, _) => OpenDashboard();
        menu.Items.Add(dashboardItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙️ Nastavení" };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "🔄 Obnovit teď" };
        refreshItem.Click += async (_, _) =>
        {
            var polling = _services.GetRequiredService<IPollingService>();
            if (polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        };
        menu.Items.Add(refreshItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ Ukončit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        // Uložit reference pro update
        _notifyIcon.ContextMenu = menu;
        _notifyIcon.TrayMouseDoubleClick += (_, _) => OpenDashboard();

        _notifyIcon.Tag = new TrayMenuRefs
        {
            BalanceItem = balanceItem,
            ToppedUpItem = toppedUpItem,
            PredictionItem = predictionItem
        };
    }

    public void UpdateTooltip(PollResult result)
    {
        if (_notifyIcon?.Tag is not TrayMenuRefs refs) return;

        var bal = result.Snapshot?.TotalBalanceDecimal ?? 0;
        var topped = result.Snapshot?.ToppedUpBalanceDecimal ?? 0;
        var pred = result.Prediction?.FormattedPrediction ?? "—";

        refs.BalanceItem.Header = $"💰 ${bal:F2} zbývá";
        refs.ToppedUpItem.Header = topped > 0
            ? $"🔁 Z toho ${topped:F2} vlastní"
            : "🔁 Všechno vlastní kredit";
        refs.PredictionItem.Header = $"📊 {pred}";

        _notifyIcon.ToolTipText = $"Zůstatek: ${bal:F2} | Predikce: {pred} | {DateTime.Now:HH:mm}";
    }

    public void ShowNotification(string message)
    {
        _notifyIcon?.ShowBalloonTip("DeepSeek Credit Check", message, BalloonIcon.Warning);
    }

    private void OpenDashboard()
    {
        var window = new DashboardWindow(_services.GetRequiredService<ViewModels.DashboardViewModel>());
        window.Show();
        window.Activate();
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_services.GetRequiredService<ViewModels.SettingsViewModel>());
        window.ShowDialog();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }

    private class TrayMenuRefs
    {
        public System.Windows.Controls.MenuItem BalanceItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem ToppedUpItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem PredictionItem { get; set; } = null!;
    }
}
```

- [ ] **Step 5: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.UI
```

Expected: Build succeeded.

- [ ] **Step 6: Commit**

```bash
git add src/DeepSeekCreditCheck.UI/
git commit -m "feat: add WPF shell with tray icon and context menu"
```

---

### Task 10: Dashboard Window s grafem

**Files:**
- Create: `src/DeepSeekCreditCheck.UI/ViewModels/BaseViewModel.cs`
- Create: `src/DeepSeekCreditCheck.UI/ViewModels/DashboardViewModel.cs`
- Create: `src/DeepSeekCreditCheck.UI/Windows/DashboardWindow.xaml`
- Create: `src/DeepSeekCreditCheck.UI/Windows/DashboardWindow.xaml.cs`
- Create: `src/DeepSeekCreditCheck.UI/Converters/BoolToColorConverter.cs`

- [ ] **Step 1: BaseViewModel**

```csharp
// src/DeepSeekCreditCheck.UI/ViewModels/BaseViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DeepSeekCreditCheck.UI.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }
}
```

- [ ] **Step 2: DashboardViewModel**

```csharp
// src/DeepSeekCreditCheck.UI/ViewModels/DashboardViewModel.cs
using System.Collections.ObjectModel;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;
using OxyPlot;
using OxyPlot.Series;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private string _currentBalance = "—";
    private string _prediction = "—";
    private string _dailySpend = "—";
    private string _lastUpdated = "—";
    private PlotModel? _balancePlot;
    private PlotModel? _spendPlot;

    public string CurrentBalance { get => _currentBalance; set => SetProperty(ref _currentBalance, value); }
    public string Prediction { get => _prediction; set => SetProperty(ref _prediction, value); }
    public string DailySpend { get => _dailySpend; set => SetProperty(ref _dailySpend, value); }
    public string LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }

    public PlotModel? BalancePlot { get => _balancePlot; set => SetProperty(ref _balancePlot, value); }
    public PlotModel? SpendPlot { get => _spendPlot; set => SetProperty(ref _spendPlot, value); }

    private readonly List<BalanceSnapshot> _history = new();

    public void OnPollCompleted(PollResult result)
    {
        if (result.Snapshot != null)
            _history.Add(result.Snapshot);

        // Keep last 30 days
        var cutoff = DateTime.UtcNow.AddDays(-30);
        _history.RemoveAll(h => h.Timestamp < cutoff);

        CurrentBalance = $"${result.Snapshot?.TotalBalanceDecimal ?? 0:F2}";
        Prediction = result.Prediction?.FormattedPrediction ?? "—";
        DailySpend = result.Prediction?.FormattedDailySpend ?? "—";
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");

        BuildBalanceChart();
        BuildSpendChart();
    }

    private void BuildBalanceChart()
    {
        var plot = new PlotModel { Title = "Zůstatek v čase" };
        var series = new LineSeries
        {
            Title = "USD",
            Color = OxyColor.FromRgb(0, 120, 215),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3
        };

        var points = _history
            .OrderBy(h => h.Timestamp)
            .Select(h => new DataPoint(
                DateTimeAxis.ToDouble(h.Timestamp.ToLocalTime()),
                (double)h.TotalBalanceDecimal))
            .ToList();

        foreach (var p in points)
            series.Points.Add(p);

        plot.Series.Add(series);
        plot.Axes.Add(new OxyPlot.Axes.DateTimeAxis
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
            StringFormat = "dd.MM."
        });

        BalancePlot = plot;
    }

    private void BuildSpendChart()
    {
        var plot = new PlotModel { Title = "Denní spotřeba (USD)" };
        var series = new ColumnSeries
        {
            FillColor = OxyColor.FromRgb(220, 80, 60),
            StrokeColor = OxyColor.FromRgb(180, 50, 30),
            StrokeThickness = 1
        };

        var sorted = _history.OrderBy(h => h.Timestamp).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            var spend = sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal;
            if (spend < 0) spend = 0;
            series.Items.Add(new ColumnItem
            {
                Value = (double)spend,
                CategoryIndex = i - 1
            });
        }

        plot.Series.Add(series);
        SpendPlot = plot;
    }
}
```

- [ ] **Step 3: DashboardWindow XAML**

```xml
<!-- src/DeepSeekCreditCheck.UI/Windows/DashboardWindow.xaml -->
<Window x:Class="DeepSeekCreditCheck.UI.Windows.DashboardWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:oxy="http://oxyplot.org/wpf"
        Title="📈 DeepSeek Kredit — Dashboard"
        Width="900" Height="650"
        MinWidth="700" MinHeight="500"
        WindowStartupLocation="CenterScreen">
    <Window.Resources>
        <Style TargetType="TextBlock" x:Key="StatLabel">
            <Setter Property="FontSize" Value="13" />
            <Setter Property="Foreground" Value="#666" />
        </Style>
        <Style TargetType="TextBlock" x:Key="StatValue">
            <Setter Property="FontSize" Value="28" />
            <Setter Property="FontWeight" Value="Bold" />
        </Style>
    </Window.Resources>
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Stats Row -->
        <Border Grid.Row="0" Background="#F5F5F5" CornerRadius="8" Padding="15" Margin="0,0,0,15">
            <UniformGrid Rows="1" Columns="4">
                <StackPanel>
                    <TextBlock Style="{StaticResource StatLabel}" Text="Aktuální zůstatek" />
                    <TextBlock Style="{StaticResource StatValue}" Text="{Binding CurrentBalance}"
                               Foreground="#0078D7" />
                </StackPanel>
                <StackPanel>
                    <TextBlock Style="{StaticResource StatLabel}" Text="Predikce" />
                    <TextBlock Style="{StaticResource StatValue}" Text="{Binding Prediction}"
                               Foreground="#107C10" />
                </StackPanel>
                <StackPanel>
                    <TextBlock Style="{StaticResource StatLabel}" Text="Denní spotřeba" />
                    <TextBlock Style="{StaticResource StatValue}" Text="{Binding DailySpend}"
                               Foreground="#D83B01" />
                </StackPanel>
                <StackPanel>
                    <TextBlock Style="{StaticResource StatLabel}" Text="Naposledy aktualizováno" />
                    <TextBlock Style="{StaticResource StatValue}" Text="{Binding LastUpdated}"
                               FontSize="20" Foreground="#333" />
                </StackPanel>
            </UniformGrid>
        </Border>

        <!-- Balance Chart -->
        <Border Grid.Row="1" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4" Margin="0,0,0,10">
            <oxy:PlotView Model="{Binding BalancePlot}" />
        </Border>

        <!-- Spend Chart -->
        <Border Grid.Row="2" BorderBrush="#E0E0E0" BorderThickness="1" CornerRadius="4">
            <oxy:PlotView Model="{Binding SpendPlot}" />
        </Border>
    </Grid>
</Window>
```

```csharp
// src/DeepSeekCreditCheck.UI/Windows/DashboardWindow.xaml.cs
using System.Windows;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
```

- [ ] **Step 4: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.UI
```

Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/DeepSeekCreditCheck.UI/ViewModels/ src/DeepSeekCreditCheck.UI/Windows/
git commit -m "feat: add dashboard window with OxyPlot charts"
```

---

### Task 11: Settings Window

**Files:**
- Create: `src/DeepSeekCreditCheck.UI/ViewModels/SettingsViewModel.cs`
- Create: `src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml`
- Create: `src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml.cs`

- [ ] **Step 1: SettingsViewModel**

```csharp
// src/DeepSeekCreditCheck.UI/ViewModels/SettingsViewModel.cs
using System.Windows.Input;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAppSettingsService _settings;
    private string _apiKey = "";
    private string _alertThreshold = "2.00";
    private int _pollingIntervalMin = 15;
    private string _status = "";

    public SettingsViewModel(IAppSettingsService settings)
    {
        _settings = settings;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        LoadCommand = new RelayCommand(async _ => await LoadAsync());
    }

    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string AlertThreshold { get => _alertThreshold; set => SetProperty(ref _alertThreshold, value); }
    public int PollingIntervalMin { get => _pollingIntervalMin; set => SetProperty(ref _pollingIntervalMin, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }

    public List<int> IntervalOptions { get; } = new() { 5, 10, 15, 30, 60 };

    public async Task LoadAsync()
    {
        var key = await _settings.GetApiKeyAsync();
        ApiKey = key ?? "";
        AlertThreshold = (await _settings.GetAlertThresholdAsync()) ?? "2.00";
        PollingIntervalMin = await _settings.GetPollingIntervalMinutesAsync();
        Status = "Načteno";
    }

    public async Task SaveAsync()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            await _settings.SetApiKeyAsync(ApiKey.Trim());
        await _settings.SetAlertThresholdAsync(AlertThreshold);
        await _settings.SetPollingIntervalMinutesAsync(PollingIntervalMin);
        Status = "✅ Uloženo";

        // Trigger re-poll with new settings
        System.Diagnostics.Process.Start(
            System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
        System.Windows.Application.Current.Shutdown();
    }
}

// Jednoduchý RelayCommand
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? param) => _canExecute?.Invoke(param) ?? true;
    public void Execute(object? param) => _execute(param);
}
```

- [ ] **Step 2: SettingsWindow XAML**

```xml
<!-- src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml -->
<Window x:Class="DeepSeekCreditCheck.UI.Windows.SettingsWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="⚙️ Nastavení"
        Width="500" Height="380"
        ResizeMode="NoResize"
        WindowStartupLocation="CenterScreen">
    <Grid Margin="20">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- API Key -->
        <StackPanel Grid.Row="0" Margin="0,0,0,15">
            <TextBlock Text="DeepSeek API Klíč" FontWeight="SemiBold" Margin="0,0,0,5" />
            <PasswordBox x:Name="ApiKeyBox"
                         Password="{Binding ApiKey, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                         Width="400" HorizontalAlignment="Left" />
            <TextBlock Text="Zadej API klíč z platform.deepseek.com/api_keys"
                       Foreground="#888" FontSize="12" Margin="0,3,0,0" />
        </StackPanel>

        <!-- Alert Threshold -->
        <StackPanel Grid.Row="1" Margin="0,0,0,15">
            <TextBlock Text="Práh pro upozornění (USD)" FontWeight="SemiBold" Margin="0,0,0,5" />
            <TextBox Text="{Binding AlertThreshold, UpdateSourceTrigger=PropertyChanged}"
                     Width="100" HorizontalAlignment="Left" />
        </StackPanel>

        <!-- Polling Interval -->
        <StackPanel Grid.Row="2" Margin="0,0,0,15">
            <TextBlock Text="Interval kontroly (minuty)" FontWeight="SemiBold" Margin="0,0,0,5" />
            <ComboBox ItemsSource="{Binding IntervalOptions}"
                      SelectedItem="{Binding PollingIntervalMin}"
                      Width="100" HorizontalAlignment="Left" />
        </StackPanel>

        <!-- Buttons -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" Margin="0,15,0,10">
            <Button Content="💾 Uložit" Command="{Binding SaveCommand}"
                    Width="100" Height="32" Margin="0,0,10,0"
                    Background="#0078D7" Foreground="White" BorderThickness="0" />
            <TextBlock Text="{Binding Status}" VerticalAlignment="Center"
                       FontSize="13" Foreground="#107C10" />
        </StackPanel>

        <TextBlock Grid.Row="4" Text="Po uložení se aplikace restartuje s novým nastavením."
                   Foreground="#888" FontSize="11" VerticalAlignment="Bottom" />
    </Grid>
</Window>
```

```csharp
// src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml.cs
using System.Windows;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}
```

- [ ] **Step 3: Ověřit build**

```bash
dotnet build src/DeepSeekCreditCheck.UI
```

Expected: Build succeeded.

- [ ] **Step 4: Commit**

```bash
git add src/DeepSeekCreditCheck.UI/ViewModels/SettingsViewModel.cs src/DeepSeekCreditCheck.UI/ViewModels/RelayCommand.cs src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml src/DeepSeekCreditCheck.UI/Windows/SettingsWindow.xaml.cs
git commit -m "feat: add settings window with API key, threshold, interval"
```

---

### Task 12: Unit testy

**Files:**
- Create: `src/DeepSeekCreditCheck.Tests/Services/PredictionEngineTests.cs`
- Create: `src/DeepSeekCreditCheck.Tests/Services/AlertServiceTests.cs`
- Create: `src/DeepSeekCreditCheck.Tests/Services/DeepSeekApiClientTests.cs`
- Create: `src/DeepSeekCreditCheck.Tests/Repositories/BalanceRepositoryTests.cs`

- [ ] **Step 1: PredictionEngine tests**

```csharp
// src/DeepSeekCreditCheck.Tests/Services/PredictionEngineTests.cs
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class PredictionEngineTests
{
    [Fact]
    public void Calculate_WithSufficientHistory_ReturnsReliablePrediction()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-7), TotalBalance = "100.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-5), TotalBalance = "90.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-3), TotalBalance = "82.00" },
            new() { Timestamp = DateTime.UtcNow.AddDays(-1), TotalBalance = "75.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "70.00" }
        };

        var result = engine.Calculate(history, 70.00m);

        Assert.True(result.IsReliable);
        Assert.True(result.DaysRemaining > 0);
        Assert.True(result.AvgDailySpend > 0);
        // ~30 spotřebováno za 7 dní => ~4.29/den => ~16 dní zbývá
        Assert.True(result.DaysRemaining > 10 && result.DaysRemaining < 25);
    }

    [Fact]
    public void Calculate_WithSingleSnapshot_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "50.00" }
        };

        var result = engine.Calculate(history, 50.00m);

        Assert.False(result.IsReliable);
        Assert.Null(result.DaysRemaining);
        Assert.Equal("—", result.FormattedPrediction);
    }

    [Fact]
    public void Calculate_WithEmptyHistory_ReturnsUnreliable()
    {
        var engine = new PredictionEngine();
        var result = engine.Calculate(new List<BalanceSnapshot>(), 10.00m);
        Assert.False(result.IsReliable);
    }

    [Fact]
    public void FormattedPrediction_Over365Days_ShowsMonths()
    {
        var engine = new PredictionEngine();
        var history = new List<BalanceSnapshot>
        {
            new() { Timestamp = DateTime.UtcNow.AddDays(-2), TotalBalance = "100.00" },
            new() { Timestamp = DateTime.UtcNow, TotalBalance = "99.99" }
        };

        var result = engine.Calculate(history, 100.00m);
        Assert.Contains("rok", result.FormattedPrediction);
    }
}
```

- [ ] **Step 2: AlertService tests**

```csharp
// src/DeepSeekCreditCheck.Tests/Services/AlertServiceTests.cs
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class AlertServiceTests
{
    [Fact]
    public void Check_BelowThreshold_TriggersAlert()
    {
        var service = new AlertService();
        AlertEventArgs? args = null;
        service.AlertTriggered += (_, e) => args = e;

        service.Check(1.50m, 2.00m);

        Assert.NotNull(args);
        Assert.Equal(1.50m, args!.CurrentBalance);
        Assert.Equal(2.00m, args.Threshold);
        Assert.Contains("$1.50", args.Message);
    }

    [Fact]
    public void Check_AboveThreshold_DoesNotTrigger()
    {
        var service = new AlertService();
        var triggered = false;
        service.AlertTriggered += (_, _) => triggered = true;

        service.Check(5.00m, 2.00m);

        Assert.False(triggered);
    }

    [Fact]
    public void Check_RepeatedlyBelowThreshold_TriggersOnlyOnSignificantDrop()
    {
        var service = new AlertService();
        var triggerCount = 0;
        service.AlertTriggered += (_, _) => triggerCount++;

        service.Check(1.50m, 2.00m); // 1st — triggers
        service.Check(1.40m, 2.00m); // < 10% drop — should not trigger
        service.Check(1.20m, 2.00m); // > 10% drop from 1.50 — triggers

        Assert.Equal(2, triggerCount);
    }

    [Fact]
    public void Check_BackAboveThreshold_ResetsState()
    {
        var service = new AlertService();
        var triggerCount = 0;
        service.AlertTriggered += (_, _) => triggerCount++;

        service.Check(1.50m, 2.00m); // triggers
        service.Check(3.00m, 2.00m); // back above
        service.Check(1.50m, 2.00m); // drops again — should trigger again

        Assert.Equal(2, triggerCount);
    }
}
```

- [ ] **Step 3: DeepSeekApiClient tests (s mock HttpClient)**

```csharp
// src/DeepSeekCreditCheck.Tests/Services/DeepSeekApiClientTests.cs
using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class DeepSeekApiClientTests
{
    [Fact]
    public async Task GetBalance_ValidResponse_ReturnsSnapshot()
    {
        var json = @"{
            ""is_available"": true,
            ""balance_infos"": [{
                ""currency"": ""USD"",
                ""total_balance"": ""103.50"",
                ""granted_balance"": ""14.50"",
                ""topped_up_balance"": ""89.00""
            }]
        }";

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json)
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        var result = await client.GetBalanceAsync("test-key");

        Assert.True(result.IsAvailable);
        Assert.Equal("USD", result.Currency);
        Assert.Equal("103.50", result.TotalBalance);
        Assert.Equal("14.50", result.GrantedBalance);
        Assert.Equal("89.00", result.ToppedUpBalance);
    }

    [Fact]
    public async Task GetBalance_Unauthorized_Throws()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized
            });

        var client = new DeepSeekApiClient(new HttpClient(handler.Object));
        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetBalanceAsync("bad-key"));
    }
}
```

- [ ] **Step 4: Spustit testy**

```bash
dotnet test src/DeepSeekCreditCheck.Tests/
```

Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add src/DeepSeekCreditCheck.Tests/
git commit -m "test: add unit tests for PredictionEngine, AlertService, ApiClient"
```

---

### Task 13: Finální sestavení a verifikace

- [ ] **Step 1: Kompletní build**

```bash
dotnet build
```

Expected: 0 Error(s), 0 Warning(s)

- [ ] **Step 2: Spustit všechny testy**

```bash
dotnet test
```

Expected: All tests pass.

- [ ] **Step 3: Vytvořit publish (single-file executable)**

```bash
dotnet publish src/DeepSeekCreditCheck.UI/DeepSeekCreditCheck.UI.csproj -c Release -r win-x64 --self-contained false -o publish/
```

- [ ] **Step 4: Ověřit že publish funguje**

```bash
ls publish/DeepSeekCreditCheck.UI.exe
```

Expected: Soubor existuje.

- [ ] **Step 5: Spustit aplikaci manuálně**

Spustit `publish/DeepSeekCreditCheck.UI.exe`, ověřit že:
- Tray ikona se objeví
- Dashboard se otevře po dvojkliku
- Nastavení se otevře po kliknutí
- Zadáním API klíče a uložením se spustí polling

- [ ] **Step 6: Commit**

```bash
git add .
git commit -m "chore: finalize build configuration and verification"
```

---

## Verification Checklist

- [ ] `dotnet build` — bez chyb
- [ ] `dotnet test` — všechny testy prochází
- [ ] Tray ikona je vidět po spuštění
- [ ] Po zadání API klíče se zobrazí zůstatek v tooltipu a menu
- [ ] Dashboard zobrazuje graf zůstatku a denní spotřeby
- [ ] Nastavení ukládá a šifruje API klíč
- [ ] Notifikace se zobrazí při poklesu pod práh
- [ ] Predikce se aktualizuje s každým pollem
