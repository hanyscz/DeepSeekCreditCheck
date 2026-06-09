# Changelog

---

## [1.1.0] — 2026-06-09

🇬🇧 **Added**
- **Today's spend** — shows how much credit has been used today (sum of positive balance deltas)
- Displayed in tray tooltip, tray context menu, and dashboard stats row
- Localized in both Czech and English

🇬🇧 **Fixed**
- **Weekly/monthly spend** calculation now correctly handles credit top-ups by summing only positive consecutive balance deltas (instead of first-last snapshot difference), preventing under-reporting or hidden values when credit is recharged within the period
- Introduced `SpendCalculator.SumPositiveDeltas()` — a reusable, tested utility replacing the broken first-last approach in all three spend calculations (today, weekly, monthly)

🇨🇿 **Přidáno**
- **Spotřeba dnes** — zobrazuje, kolik kreditu se dnes utratilo (součet kladných delta zůstatku)
- Zobrazeno v tooltipu traye, kontextovém menu traye a v řádku statistik na dashboardu
- Lokalizováno v češtině i angličtině

🇨🇿 **Opraveno**
- **Výpočet týdenní/měsíční/dnešní spotřeby** nyní správně pracuje s dobíjením kreditu — sčítá pouze kladné mezisnapshotové rozdíly (místo rozdílu první minus poslední snapshot), takže dobíjení nezkresluje výsledky
- Přidán `SpendCalculator.SumPositiveDeltas()` — znovupoužitelná, otestovaná utilita nahrazující chybný first-last přístup ve všech třech výpočtech spotřeby

---

## [1.0.0] — 2026-06-07

🇬🇧 **Initial release**
- Balance monitoring via system tray
- Dashboard with hourly spend chart, daily/weekly/monthly stats
- Prediction engine estimating remaining days
- Low-balance alert notifications
- Dark theme UI
- Multi-language support (CS/EN)
- SQLite history with data browser

🇨🇿 **První vydání**
- Monitorování zůstatku v systémové trayi
- Dashboard s grafem hodinové spotřeby, denní/týdenní/měsíční statistiky
- Predikční engine odhadující zbývající dny
- Upozornění při nízkém kreditu
- Tmavý motiv UI
- Vícejazyčná podpora (CS/EN)
- Historie v SQLite s prohlížečem dat
