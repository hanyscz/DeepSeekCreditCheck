# Changelog – DeepSeek Credit Checker

## v1.6.0 (2026-06-18)

### ✨ New Features

* **📅 Monthly Navigation for Platform Stats** – Added previous (`◀`) and next (`▶`) monthly navigation buttons under the month heading in the DeepSeek Platform token usage card, allowing users to browse token consumption data from prior months.
* **🔒 Future Protection** – Next button is automatically disabled when viewing the current month to prevent future date navigation.
* **⚡ CommandManager Integration** – Refactored `RelayCommand` to route `CanExecuteChanged` through WPF `CommandManager.RequerySuggested` for real-time button state updates and resolved build warnings.

---

## v1.5.0 (2026-06-18)

### ✨ New Features

* **🔑 DeepSeek Platform login and token usage** – integrated detailed tracking of session token usage directly from the DeepSeek Platform (`platform.deepseek.com`) inside the dashboard, utilizing system WebView2 to securely capture cookies/tokens.
* **📊 Detailed model statistics table** – replaced simple totals with a comprehensive grid showing detailed breakdown (Input Miss, Cache Hit, Output, Total, and Costs in USD rounded to 2 decimal places) for `Pro` (and general) models and `Flash` models separately, plus a `Celkem` (Total) row.
* **📏 Dynamic dashboard layout adjustments** – resized the platform card height (to 130px) and adjusted grid row spacing to fit the detailed model statistics cleanly and premium.

### 🛠️ Tech Stack

* **🧩 DeepSeekPlatformClient (IDeepSeekPlatformClient)** – new client utilizing its own isolated `HttpClient` (independent of public API endpoints) to fetch `/api/v0/users/get_user_summary`, `/api/v0/usage/amount`, and `/api/v0/usage/cost`.
* **🧩 Array-aware JSON node resolver** – extended `GetSafeNode` to support index parsing of array nodes (e.g. `"0"`), preventing double-counting bugs caused by recursive parsing of daily (`days`) and total (`total`) entries from the API responses.
* **🧪 80 unit tests** – 10 unit tests verifying platform client API interaction, usage amount/cost parsing logic, and array-index routing.

---

## v1.4.0 (2026-06-10)

### ✨ New Features

* **🎨 Balance in tray icon** – the tray icon now renders the current balance as text with a status color: green (OK), orange (≤ 2× threshold), red (below threshold), blue with "$" (unknown). Values under $10 show one decimal (e.g. "1.5"), $100+ shows "99+".
* **🚀 Start with Windows** – new checkbox in Settings registers the app in the `HKCU\...\Run` registry key. The registry is the single source of truth – the checkbox always reflects the actual state.
* **🔑 Test API key button** – validate the API key directly in Settings without waiting for the next poll. Shows balance on success, distinguishes invalid key (401) from network errors.
* **💚 Recharge detection** – when the balance jumps up between polls, a positive toast appears: "Credit recharged (+$X) — currently $Y".
* **🔒 Single instance** – a second launch of the app exits silently (named mutex), preventing duplicate polling and tray icons.

### 🛠️ Tech Stack

* **🧩 TrayIconFormatter** – new pure logic class in Core (status + icon text) separated from GDI rendering for unit testability.
* **🧩 StartupService (IStartupService)** – registry-based autostart management with error logging.
* **🧩 PollingService** – new `RechargeDetected` event (`RechargeEventArgs`: Amount, NewBalance); `PollResult` now carries `Threshold` for icon coloring.
* **🧩 Icon lifecycle** – dynamically generated 32×32 icons properly destroy the unmanaged HICON (`DestroyIcon`) and dispose the previous icon on swap – no GDI handle leaks.
* **🧪 70 tests** – 16 new (TrayIconFormatter status/text matrix, recharge detection incl. first-poll and noise-delta edge cases, threshold propagation).

---

## v1.3.0 (2026-06-10)

### 🔧 Spend Calculation Fixes

* **🐛 DateTime Kind mismatch fixed** – SQLite timestamps ("Unspecified") now auto-convert to UTC so `ToLocalTime()` works consistently. Eliminates incorrect assignment of records to calendar days.
* **📊 Today's spend fixed** – calculated as `SumPositiveDeltas` for today's local calendar day. Correctly ignores top-ups within the day.
* **📈 Weekly/monthly stats fixed** – per-day aggregation (SumPositiveDeltas per calendar day) instead of a flat sum over the period. Eliminates distortion from cross-midnight deltas and timestamp jitter.
* **🎯 Prediction refined** – average daily spend is computed only from complete calendar days (≥12h data span). Partial days (e.g. Jun 07 with only 5h of data) are excluded. Range (~N-M days) is only shown when 3+ full days are available.

### ✨ New Features

* **📥 CSV Export** – a "📥 Export CSV" button in the Data Browser window to export balance history to a semicolon-delimited CSV file.

### 🛠️ Tech Stack

* **🧩 BalanceSnapshot** – `Timestamp` setter now normalizes `DateTimeKind.Unspecified` to `DateTimeKind.Utc`.
* **🧩 PredictionEngine** – `AggregateDailySpend` uses local date filtering, 12h threshold for full-day eligibility, and `SumPositiveDeltas` per day.
* **🧩 DashboardViewModel** – new `SumSpendByDay` helper for correct per-day aggregation in weekly/monthly stats.
* **🧪 54 tests** – 2 new prediction tests (real-world data + range for 3+ full days).

---

## v1.2.0 (2026-06-09)

### ✨ Core Features

* **🔄 Automatic updates via GitHub Releases** – the app now checks for new versions on startup and periodically every 4 hours; when a newer release is found, a notification toast appears with a download button, and the tray context menu shows the available version
* **📥 One-click install** – clicking "Download" streams the release ZIP, extracts it, replaces all application files via a batch script, and automatically restarts with the new version
* **🟢 Update banner in Dashboard** – when an update is available, the Dashboard shows a green banner with the version number and a download button
* **🔹 Version info in Settings** – the bottom of the Settings window now shows the current application version (e.g. `🔹 v1.2.0`)

### 🛠️ Tech Stack

* **♻️ GitHub Actions release workflow** – manual `workflow_dispatch` trigger builds, tests, publishes, zips, and creates a GitHub Release with tag automatically; no push/PR automation keeps releases fully manual
* **🧩 IUpdateService / UpdateService** – new service in Core checking `GET /repos/{owner}/{repo}/releases/latest` (no token needed), streaming ZIP downloads with progress, batch script generation, and success-marker file for post-update confirmation toast
* **🪝 UpdateAvailable event** – DashboardViewModel subscribes reactively; the update banner appears even if the dashboard was opened before the first check completed

---

## v1.1.0 (2026-06-09)

### ✨ Core Features

* **📊 Today's spend** – added tracking of credit spent during the current day (calculated as the sum of positive balance deltas); displayed in the tray tooltip, tray context menu, and dashboard statistics row
* **📈 Improved spend calculation** – weekly, monthly, and daily spend statistics now correctly handle credit top-ups by summing consecutive balance deltas instead of taking a simple first-last snapshot difference, preventing under-reporting when credit is recharged

### 🛠️ Tech Stack

* **🧮 SpendCalculator** – introduced a reusable, fully tested `SumPositiveDeltas()` utility to centralize and fix balance logic across all tracking features

---

## v1.0.0 (2026-06-07)

### ✨ Core Features

* **💰 System tray balance monitor** – lightweight WPF app checking DeepSeek API credit balance periodically
* **📊 Dashboard** – hourly spend chart (OxyPlot), daily/weekly/monthly stats
* **📈 Prediction engine** – estimates remaining days based on average daily spend
* **⚠️ Alert notifications** – custom dark-themed toast when balance drops below configurable threshold
* **🔒 Secure storage** – API key encrypted with Windows DPAPI
* **🌐 Multi-language** – Czech and English built-in, extensible via JSON files
* **📁 Data browser** – view and delete historical balance records
* **🌙 Dark theme** – all windows with modern dark design

### 🛠️ Tech Stack

* **WPF (.NET 8)** – tray icon via Hardcodet.NotifyIcon.Wpf, charts via OxyPlot.Wpf
* **SQLite** – local database via Microsoft.Data.Sqlite + Dapper
* **DI container** – Microsoft.Extensions.DependencyInjection
* **GitHub Actions CI** – manual `workflow_dispatch` build, test, publish pipeline
