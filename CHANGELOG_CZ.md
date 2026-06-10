# Changelog CZ – DeepSeek Credit Checker

## v1.3.0 (2026-06-10)

### 🔧 Opravy výpočtů spotřeby

* **🐛 DateTime Kind mismatch opraven** – timestampy z SQLite ("Unspecified") se nyní převádí na UTC, aby `ToLocalTime()` všude fungoval konzistentně. Odstraňuje nesprávné přiřazení záznamů do kalendářních dnů.
* **📊 Dnešní spotřeba opravena** – počítá se jako `SumPositiveDeltas` za dnešní lokální kalendářní den. Správně ignoruje dobíjení.
* **📈 Týdenní/měsíční statistiky opraveny** – agregace po jednotlivých kalendářních dnech (per-day SumPositiveDeltas) místo celoplošného součtu. Eliminuje zkreslení z cross-midnight delt a kolísání timestampů.
* **🎯 Predikce zpřesněna** – průměrná denní spotřeba se počítá pouze z plných kalendářních dnů (≥12h rozpětí dat). Částečné dny (např. 07.06. s pouhými 5h dat) jsou vyřazeny. Rozsah ~N-M dní se zobrazuje až od 3+ plných dnů.

### ✨ Nové funkce

* **📥 Export CSV** – v okně Prohlížeče dat přibylo tlačítko "📥 Export CSV" pro export historie zůstatků do souboru CSV s oddělovačem `;`.

### 🛠️ Technický stack

* **🧩 BalanceSnapshot** – vlastnost `Timestamp` nyní normalizuje `DateTimeKind.Unspecified` na `DateTimeKind.Utc` při setu.
* **🧩 Predikce** – `AggregateDailySpend` používá lokální datum, 12h filtr na nekompletní dny, `SumPositiveDeltas` per day.
* **🧩 Statistiky** – nová metoda `SumSpendByDay` pro správnou per-day agregaci v týdenních/měsíčních výpočtech.
* **🧪 54 testů** – 2 nové testy predikce (reálná data + range pro 3+ dny).

---

## v1.2.0 (2026-06-09)

### ✨ Hlavní funkce

* **🔄 Automatické aktualizace z GitHub Releases** – aplikace nyní kontroluje nové verze při startu a periodicky každé 4 hodiny; při nalezení novější verze zobrazí notifikační toast s tlačítkem ke stažení a v tray menu zpřístupní položku s verzí ke stažení
* **📥 Instalace na jedno kliknutí** – po kliknutí na "Stáhnout" se streamuje ZIP z GitHub Release, extrahuje, přes batch skript nahradí soubory aplikace a automaticky restartuje s novou verzí
* **🟢 Banner v Dashboardu** – při dostupnosti nové verze se na Dashboardu zobrazí zelený banner s číslem verze a tlačítkem ke stažení
* **🔹 Verze v Nastavení** – ve spodní části okna Nastavení se zobrazuje aktuální verze aplikace (např. `🔹 v1.2.0`)

### 🛠️ Technický stack

* **♻️ GitHub Actions release workflow** – manuální trigger `workflow_dispatch` buildne, otestuje, publikuje, zazipuje a vytvoří GitHub Release s tagem automaticky; bez push/PR automatizace zůstávají release plně manuální
* **🧩 IUpdateService / UpdateService** – nová služba v Core kontrolující `GET /repos/{owner}/{repo}/releases/latest` (bez tokenu), streamované stahování ZIP s progressem, generování batch skriptu a marker soubor pro potvrzení úspěšné aktualizace
* **🪝 UpdateAvailable event** – DashboardViewModel reaguje reaktivně; banner se zobrazí, i když byl dashboard otevřen dřív než proběhla první kontrola

---

## v1.1.0 (2026-06-09)

### ✨ Hlavní funkce

* **📊 Dnešní spotřeba** – přidáno sledování kreditu utraceného za aktuální den (počítáno jako součet kladných rozdílů zůstatku); zobrazeno v tooltipu traye, kontextovém menu traye a v řádku statistik na dashboardu
* **📈 Přesnější výpočet spotřeby** – statistiky denní, týdenní a měsíční spotřeby nyní správně pracují s dobíjením kreditu; sčítají se pouze konkrétní mezizáznamové úbytky namísto prostého rozdílu mezi prvním a posledním snapshotem, což eliminuje zkreslení statistik při dobití peněženky

### 🛠️ Technický stack

* **🧮 SpendCalculator** – implementována znovupoužitelná a otestovaná utilita `SumPositiveDeltas()`, která sjednocuje a opravuje logiku výpočtu spotřeby napříč celou aplikací

---

## v1.0.0 (2026-06-07)

### ✨ Hlavní funkce

* **💰 Monitorování zůstatku v tray** – odlehčená WPF aplikace periodicky kontrolující DeepSeek API kredit
* **📊 Dashboard** – graf hodinové spotřeby (OxyPlot), statistiky den/týden/měsíc
* **📈 Predikce** – odhad zbývajících dní podle průměrné denní spotřeby
* **⚠️ Upozornění** – vlastní tmavý toast při poklesu pod nastavitelný práh
* **🔒 Bezpečné úložiště** – API klíč šifrovaný Windows DPAPI
* **🌐 Vícejazyčnost** – čeština a angličtina, rozšiřitelná přes JSON soubory
* **📁 Prohlížeč dat** – prohlížení a mazání historických záznamů
* **🌙 Tmavý režim** – všechna okna v moderním dark designu

### 🛠️ Technický stack

* **WPF (.NET 8)** – tray ikona přes Hardcodet.NotifyIcon.Wpf, grafy přes OxyPlot.Wpf
* **SQLite** – lokální databáze přes Microsoft.Data.Sqlite + Dapper
* **DI container** – Microsoft.Extensions.DependencyInjection
* **GitHub Actions CI** – manuální `workflow_dispatch` build, test, publish pipeline
