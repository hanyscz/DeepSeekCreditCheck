# Changelog CZ – DeepSeek Credit Checker

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
