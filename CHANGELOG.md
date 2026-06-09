# Changelog – DeepSeek Credit Checker

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
