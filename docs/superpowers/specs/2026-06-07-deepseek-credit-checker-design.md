# DeepSeek Credit Checker — Design Spec

**Autor:** Hanys + Claude
**Datum:** 2026-06-07
**Stav:** Schváleno k implementaci

## 1. Overview

WPF tray aplikace pro Windows, která monitoruje stav kreditu na DeepSeek API účtu. Volá oficiální endpointy DeepSeek API v pravidelných intervalech, ukládá historii do lokální SQLite databáze, zobrazuje stav v systémové trayi a v dashboard okně s grafem. Poskytuje predikci, na jak dlouho kredit při aktuální spotřebě vydrží, a notifikuje při poklesu pod nastavenou mez.

## 2. Technology Stack

| Vrstva | Technologie | Verze |
|--------|-------------|-------|
| Runtime | .NET | 8.0 |
| UI Framework | WPF | .NET 8 |
| Databáze | SQLite | Microsoft.Data.Sqlite |
| ORM | EF Core | 8.x |
| Grafy | OxyPlot.Wpf | 2.x |
| Tray ikona | Hardcodet.NotifyIcon.Wpf | 1.x |
| Notifikace | Microsoft.Toolkit.Uwp.Notifications | 7.x |
| HTTP klient | System.Net.Http.HttpClient | built-in |
| Šifrování klíče | DPAPI (Data Protection API) | built-in |

## 3. Architecture — Komponenty

```
DeepSeekCreditCheck.sln
├── DeepSeekCreditCheck.Core/         ← Business logic (NET 8 class library)
│   ├── Models/                       ← BalanceSnapshot, UsageRecord, AppSettings
│   ├── Services/
│   │   ├── DeepSeekApiClient.cs      ← HTTP client pro DeepSeek API
│   │   ├── PollingService.cs         ← Periodický timer, orchestrátor
│   │   ├── PredictionEngine.cs       ← Výpočet predikce spotřeby
│   │   └── AlertService.cs           ← Kontrola mezi, vyvolání notifikace
│   ├── Data/
│   │   ├── AppDbContext.cs           ← EF Core DbContext
│   │   └── Repositories/            ← BalanceRepository, UsageRepository
│   └── Configuration/
│       └── AppSettingsService.cs     ← Key-value nastavení, DPAPI šifrování
├── DeepSeekCreditCheck.UI/           ← WPF aplikace
│   ├── App.xaml(.cs)
│   ├── MainWindow.xaml(.cs)          ← Hlavní okno (skryté, jen tray)
│   ├── Windows/
│   │   ├── DashboardWindow.xaml      ← Graf + statistiky
│   │   └── SettingsWindow.xaml       ← Konfigurace
│   ├── ViewModels/
│   │   ├── TrayViewModel.cs
│   │   ├── DashboardViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Services/
│   │   └── TrayIconService.cs        ← Hardcodet NotifyIcon management
│   └── Converters/
│       └── BalanceToColorConverter.cs
└── DeepSeekCreditCheck.Tests/        ← Unit testy (xUnit)
```

### Data flow

```
┌──────────────────────────────────────────────────────────┐
│ Každých N minut (konfigurovatelné, default 15):         │
│                                                          │
│ 1. PollingService.OnTimerTick()                          │
│ 2. DeepSeekApiClient.GetBalanceAsync()  ──►  GET /user/balance
│ 3. DeepSeekApiClient.GetUsageAsync()    ──►  GET /v1/usage
│ 4. Repository.SaveSnapshotAsync(...)                     │
│ 5. Repository.SaveUsageRecordAsync(...)                   │
│ 6. PredictionEngine.Calculate(history, currentBalance):  │
│    • avgDailySpend = sum7d / 7                           │
│    • predictedDays = currentBalance / avgDailySpend       │
│ 7. AlertService.Check(currentBalance, threshold):        │
│    • if < threshold → Windows Toast notifikace           │
│ 8. TrayIconService.TryUpdateTooltip(...)                  │
│ 9. DashboardViewModel.Refresh() — je-li okno otevřeno    │
└──────────────────────────────────────────────────────────┘
```

## 4. Datový model

### SQLite tabulky

```sql
-- Snapshot zůstatku z každého API volání
CREATE TABLE BalanceSnapshots (
    SnapshotId   INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp    TEXT NOT NULL,           -- ISO 8601 UTC
    IsAvailable  INTEGER NOT NULL,       -- 0/1 boolean
    Currency     TEXT NOT NULL,           -- "USD" / "CNY"
    TotalBalance TEXT NOT NULL,           -- decimal as string
    GrantedBalance TEXT NOT NULL,
    ToppedUpBalance TEXT NOT NULL
);

-- Usage záznam z každého API volání
CREATE TABLE UsageRecords (
    RecordId     INTEGER PRIMARY KEY AUTOINCREMENT,
    Timestamp    TEXT NOT NULL,           -- ISO 8601 UTC
    PeriodStart  TEXT,                    -- rozsah dotazu
    PeriodEnd    TEXT,
    TotalTokens  INTEGER NOT NULL,
    InputTokens  INTEGER NOT NULL,
    OutputTokens INTEGER NOT NULL,
    CachedTokens INTEGER                  -- nullable — nemusí být vždy k dispozici
);

-- Aplikační nastavení
CREATE TABLE AppSettings (
    Key   TEXT PRIMARY KEY,
    Value TEXT NOT NULL
);
```

### Předdefinované klíče v AppSettings

| Klíč | Default | Popis |
|------|---------|-------|
| `ApiKey` | — | DeepSeek API klíč (šifrovaný DPAPI) |
| `AlertThreshold` | `2.00` | Notifikační práh v USD |
| `PollingIntervalMin` | `15` | Interval pollingu v minutách |
| `FirstRunComplete` | `false` | Zda proběhl první setup |

## 5. Tray ikona

```
Pravé kliknutí na ikonu:
┌─────────────────────────────────┐
│ 💰 $103.50 zbývá               │ ← disabled, info (celkem)
│ 🔁 Z toho $89.00 vlastní      │ ← disabled, topped_up
│ 📊 ~34 dní                     │ ← disabled, predikce
│ ───────────────────────────── │
│ 📈 Dashboard                   │ ← otevře graf/statistiky
│ ⚙️ Nastavení                  │ ← konfigurace
│ 🔄 Obnovit teď                 │ ← force refresh
│ ───────────────────────────── │
│ ❌ Ukončit                     │
└─────────────────────────────────┘
```

Tooltip při najetí myší: `Zůstatek: $103.50 | Predikce: ~34 dní | Naposledy: 15:42`

## 6. Dashboard Window

Layout:
- **Nahoře:** Karta s aktuálním zůstatkem, predikcí, daily spend
- **Uprostřed:** Graf zůstatku v čase (OxyPlot LineSeries + AreaSeries)
- **Dole:** Graf denní spotřeby (OxyPlot ColumnSeries)

## 7. Settings Window

- API klíč (password box)
- Práh pro notifikaci (číselník, default $2.00)
- Interval pollingu (dropdown: 5/10/15/30/60 minut)

## 8. API Client — Detaily

### Balance endpoint

```
GET https://api.deepseek.com/user/balance
Authorization: Bearer {API_KEY}

Response:
{
  "is_available": true,
  "balance_infos": [
    { "currency": "USD", "total_balance": "103.50",
      "granted_balance": "14.50", "topped_up_balance": "89.00" }
  ]
}
```

### Usage endpoint (volat s opatrností — potřeba otestovat)

```
GET https://api.deepseek.com/v1/usage?start_time=2026-05-31&end_time=2026-06-07
Authorization: Bearer {API_KEY}

Response (očekávaný formát):
{
  "total_usage": { "total_tokens": ..., "input_tokens": ..., "output_tokens": ... },
  "details": [...]
}
```

> ⚠️ **Fallback:** Pokud `/v1/usage` nebude dostupné, počítáme spotřebu z rozdílu zůstatků (delta balance mezi snapshoty).

## 9. Predikční engine

```
avgDailySpend = AVG(denní spotřeba za posledních 7 dní)
    // Denní spotřeba = rozdíl balance snapshotů / počet dní

predictedDays = currentTotalBalance / avgDailySpend

// Zohlednění volatility:
// Pokud std dev > 30 % průměru → zobrazit jako "~12-18 dní" (pásmo)
```

## 10. Notifikace

- Toast notifikace: "⚠️ DeepSeek kredit klesl pod $2.00 — aktuálně $1.87"
- Akce v notifikaci: "Dobít kredit" → otevře `https://platform.deepseek.com/top_up` v prohlížeči

## 11. Verification

Co testovat:
1. **Unit testy:** PredictionEngine, AlertService, DeepSeekApiClient (mockovaný HttpMessageHandler)
2. **Integrace:** AppDbContext migrace a operace, Repository vrstvy
3. **Manuální:** Spustit aplikaci, zadat API klíč, ověřit že se načte zůstatek a zobrazí v trayi
4. **Edge cases:** Chybějící API klíč, API vrátí 401/403/429, chybějící internet
5. **Long run:** Nechat běžet několik hodin s krátkým intervalem, ověřit že se data správně ukládají

## 12. Open Issues

- [ ] Ověřit endpoint `GET /v1/usage` — zda existuje a v jakém formátu vrací data
- [ ] Rozhodnout zda použít EF Core nebo Dapper (menší overhead pro jednoduché schéma)
- [ ] Ikona aplikace — SVG/ICO resource
