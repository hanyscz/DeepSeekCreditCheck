# Changelog CZ – DeepSeek Credit Checker

## v1.8.0 (2026-06-24)

### ✨ Nové funkce

* **📅 Denní statistiky spotřeby modelů** – Na hlavní Dashboard WPF aplikace byl přidán nový přehledný blok „Dnešní spotřeba“. Zobrazuje detailní tokeny (Input Miss, Cache Hit, Output, Celkem) a náklady (USD) rozdělené podle modelů (Pro, Flash, Celkem) pro aktuální kalendářní den. Zobrazuje se dynamicky pouze při prohlížení aktuálního měsíce.

### 🛠️ Technický stack

* **🧩 Zpracování denních dat v paměti** – Využívá již stahované denní rozpisy statistik (`days` pole v JSON odpovědích billing API DeepSeek) bez nutnosti provádět další síťové požadavky.
* **🧪 Testy ViewModelu** – Přidány nové jednotkové testy ověřující správné parsování denních statistik v `DashboardViewModel` a jejich dynamické zobrazení/skrytí v závislosti na vybraném měsíci.

---

## v1.7.0 (2026-06-18)

### ✨ Nové funkce

* **📊 Podrobné statistiky spotřeby a nákladů (Export ZIP / CSV)** – Přidáno nové tlačítko "📊 Podrobné statistiky", které otevírá samostatný detailní panel. Umožňuje uživateli na jedno kliknutí stáhnout oficiální detailní export z platformy (ZIP archiv s CSV soubory pro spotřebu a náklady).
* **💾 Lokální SQLite Caching** – Parsery načtou stažené CSV soubory a uloží je do lokální SQLite databáze. Tím se vytvoří stálá cache, takže historické měsíce lze prohlížet offline a bez opakovaného stahování. Při opětovném stažení se stará data daného měsíce bezpečně přepíší.
* **🔑 Rozdělení API klíčů podle modelů** – Seskupuje a rozděluje spotřebu pro stejný API klíč, pokud je použit napříč různými modely (např. Pro a Flash). V tabulkách i grafech se tak zobrazují jako samostatné položky.
* **📊 Sloupcový graf API klíčů a formátování** – Nahrazení původního koláčového grafu API klíčů přehledným vodorovným sloupcovým grafem seřazeným podle nákladů (kdo vede). Všechny číselné hodnoty v tabulkách statistik jsou nyní zarovnány doprava pro lepší čitelnost a veškeré USD náklady jsou zaokrouhlovány na 2 desetinná místa.
* **📈 Interaktivní grafy (OxyPlot)** – Vizualizace denního vývoje nákladů rozdělených podle API klíčů a meziměsíční srovnání celkových nákladů pomocí moderních tmavě laděných grafů.
* **🖥️ Detailní WPF Dashboard** – Zbrusu nové okno `DetailedStatsWindow` s širším rozvržením, navigací měsíců pomocí šipek (◀ / ▶) s blokováním do budoucna, detailním zobrazením velkých čísel s oddělovači tisíců a kompletní meziměsíční srovnávací tabulkou s rozpadem tokenů.

### 🛠️ Technický stack

* **🧩 SQLite Repozitář** – Vytvořena tabulka `MonthlyUsageDetails` a indexy pro rychlé dotazování. Implementováno úložiště `UsageRepository` pro asynchronní operace ukládání a čtení detailních statistik.
* **🧩 ZIP & CSV In-Memory Parser** – Třída `UsageCsvParser` zpracovává ZIP archivy kompletně v paměti a bezpečně parsuje CSV řádky i v přítomnosti uvozovek a různých číselných formátů.
* **🧩 Vylepšení vzhledu a formátování** – Stylování tabulek pomocí WPF `CellTemplate` s `TextBlock` `HorizontalAlignment="Right"` pro zarovnání čísel doprava a úprava vlastností modelů pro formátování nákladů s přesností na 2 desetinná místa (`F2`).
* **🧩 OxyPlot 2.0+ Stabilizace** – Oprava chyb sestavení vyvolaných odstraněním `ColumnSeries` (nahrazeno `BarSeries` s transposed osami) a odstraněním vlastností legend přímo v `PlotModel` (nahrazeno kolekcí `Legends`).
* **🧪 Unit testy** – Přidány testy ověřující správnost parsování CSV a ukládání/načítání dat z repozitáře.

---

## v1.6.0 (2026-06-18)

### ✨ Nové funkce

* **📅 Měsíční navigace pro statistiky platformy** – Pod nadpis měsíce v kartě spotřeby platformy DeepSeek byla přidána navigační tlačítka pro posun zpět (`◀`) a vpřed (`▶`), která uživatelům umožňují prohlížet spotřebu tokenů za předchozí měsíce.
* **🔒 Ochrana před budoucností** – Tlačítko pro posun vpřed se automaticky zakáže při zobrazení aktuálního měsíce, aby se zabránilo navigaci do budoucích měsíců.
* **⚡ Integrace s CommandManager** – Třída `RelayCommand` byla upravena tak, aby směrovala událost `CanExecuteChanged` přes WPF `CommandManager.RequerySuggested`. Tím se vyřešilo varování při kompilaci a tlačítka se v UI aktivují/deaktivují v reálném čase.

---

## v1.5.0 (2026-06-18)

### ✨ Nové funkce

* **🔑 Přihlášení a spotřeba na platformě DeepSeek** – integrované podrobné sledování spotřeby tokenů přímo z platformy DeepSeek (`platform.deepseek.com`) na dashboardu pomocí systémového WebView2 pro bezpečné získání cookies/tokenů.
* **📊 Detailní tabulka statistik modelů** – nahrazení jednoduchých souhrnů přehlednou tabulkou s podrobným rozpisem (Input Miss, Cache Hit, Output, Celkem a náklady v USD zaokrouhlené na 2 desetinná místa) pro modely `Pro` (a obecné) a `Flash` samostatně, plus řádek `Celkem`.
* **📏 Dynamické úpravy rozvržení dashboardu** – upravená výška karty platformy (na 130px) a rozestupy řádků tabulky, aby se podrobné statistiky modelů vešly čistě a prémiově.

### 🛠️ Technický stack

* **🧩 DeepSeekPlatformClient (IDeepSeekPlatformClient)** – nový klient využívající vlastní izolovaný `HttpClient` (nezávislý na veřejných API endpointech) pro získání `/api/v0/users/get_user_summary`, `/api/v0/usage/amount` a `/api/v0/usage/cost`.
* **🧩 Parsování polí v JSON** – rozšíření metody `GetSafeNode` o podporu indexů prvků pole (např. `"0"`), což předchází chybám s dvojitým započítáváním nákladů způsobeným rekurzivním parsováním denních (`days`) a celkových (`total`) položek z odpovědí API.
* **🧪 80 jednotkových testů** – 10 nových jednotkových testů ověřujících komunikaci klienta s API platformy, logiku parsování spotřeby/cen a směrování indexů polí.

---

## v1.4.0 (2026-06-10)

### ✨ Nové funkce

* **🎨 Zůstatek v tray ikoně** – tray ikona nyní vykresluje aktuální zůstatek jako text s barvou dle stavu: zelená (OK), oranžová (≤ 2× práh), červená (pod prahem), modrá s "$" (neznámý stav). Hodnoty pod $10 s jedním desetinným místem (např. "1.5"), $100+ jako "99+".
* **🚀 Spuštění při startu Windows** – nový checkbox v Nastavení registruje aplikaci v registru `HKCU\...\Run`. Registr je jediný zdroj pravdy – checkbox vždy odráží skutečný stav.
* **🔑 Tlačítko Otestovat klíč** – ověření API klíče přímo v Nastavení bez čekání na další poll. Při úspěchu ukáže zůstatek, rozlišuje neplatný klíč (401) od síťové chyby.
* **💚 Detekce dobití kreditu** – při kladném skoku zůstatku mezi polly se zobrazí pozitivní toast: „Kredit dobit (+$X) — aktuálně $Y“.
* **🔒 Jediná instance** – druhé spuštění aplikace se tiše ukončí (named mutex), zabraňuje zdvojenému pollingu a duplicitním tray ikonám.

### 🛠️ Technický stack

* **🧩 TrayIconFormatter** – nová čistá logika v Core (stav + text ikony) oddělená od GDI vykreslování kvůli testovatelnosti.
* **🧩 StartupService (IStartupService)** – správa autostartu přes registry s logováním chyb.
* **🧩 PollingService** – nový event `RechargeDetected` (`RechargeEventArgs`: Amount, NewBalance); `PollResult` nyní nese `Threshold` pro barvení ikony.
* **🧩 Životní cyklus ikon** – dynamicky generované 32×32 ikony korektně ničí nespravovaný HICON (`DestroyIcon`) a při výměně disposují předchozí ikonu – žádné GDI handle leaky.
* **🧪 70 testů** – 16 nových (matice stavů/textů TrayIconFormatter, detekce dobití vč. edge cases první poll a šumová delta, propagace prahu).

---

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
