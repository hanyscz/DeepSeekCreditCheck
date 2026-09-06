# DeepSeek Credit Checker

**Monitor your DeepSeek API credit balance from the Windows system tray.**  
*v1.11.0 — released 2026-09-06*

---

🇬🇧 **English**

A lightweight Windows application that runs in the system tray and periodically checks your DeepSeek API account balance. Shows your balance directly on the tray icon with a color indicator, displays details on hover, predicts remaining credit days, and alerts you when balance drops below a configurable threshold.

### Features

- **🎨 Balance on tray icon** — the icon dynamically shows your current balance as a number with status colors: green (OK), orange (approaching threshold), red (critical), blue (awaiting key)
- **🔥 Peak / Off-Peak tariff tracking** — real-time monitoring of DeepSeek API rate tiers (Peak: 01:00–04:00 & 06:00–10:00 UTC at standard rates; Off-Peak: 50% discount on all tokens). Displayed via a badge dot on the tray icon (🔴 Red / 🟢 Green), tooltips, context menu, and a dedicated dashboard card with countdowns
- **⚡ Peak / Off-Peak analytics in Detailed Statistics** — dedicated tab providing complete breakdown of Peak vs. Off-Peak token usage, cost ratios, exact savings calculated from 50% discount windows, daily trend curves across all days of the month, and granular model-by-tier tables based on exact billing prices
- **💰 Balance in hover** — tooltip shows current balance, today's spend, prediction, tariff tier, and last update time
- **📋 In-app Changelog** — explore release notes and updates directly in the app via the dashboard button, tray context menu, or automatic post-update popup
- **📊 Dashboard** — hourly spend chart with calendar-day aggregation, daily/weekly/monthly spend stats, and dedicated tariff banner
- **📈 Prediction** — estimates remaining days based on average daily spend per calendar day
- **⚠️ Custom notification toast** — dark-themed popup in bottom-right corner with fade-in animation
- **🔒 Secure** — API key encrypted with Windows DPAPI
- **🌐 Multi-language** — Czech and English built-in; add your own via JSON files in `Lang/`
- **📁 Data browser** — view and delete historical balance records with multi-select
- **📝 Logging** — errors logged to file; configurable log path for multi-PC sync
- **🗄️ Configurable DB path** — share database across PCs via network drive or cloud sync
- **🔄 Auto-update** — checks for new releases on startup and every 4 hours; one-click download and install from GitHub Releases
- **🚀 Start with Windows** — optional autostart via a checkbox in Settings
- **💚 Recharge detection** — when you top up your credit a positive toast confirms the new balance
- **🔑 DeepSeek Platform integration** — securely log in to the DeepSeek Platform via WebView2 to track detailed monthly and daily token usage (Input Miss, Cache Hit, Output, Total) and exact costs rounded to 2 decimal places in USD split by Pro, Flash, and Vision models directly on the dashboard. Navigation arrows allow you to view historical usage data for previous months, with a dedicated daily usage block visible for the current month.
- **📊 Detailed Statistics Dashboard** — download the official ZIP export containing CSV files on-demand, parse and store details locally in SQLite, and visualize daily cost trends and monthly comparisons via interactive OxyPlot charts with modern dark tooltips in a dedicated window. The API Key view features a horizontal bar chart sorted by cost and groups same-key usage by model.
- **🌙 Dark theme** — all windows with modern dark design
- **🏡 Home Assistant Integration** — monitor your DeepSeek balance and detailed usage directly in Home Assistant (custom component included)

### How to use

1. Download the latest build from [GitHub Releases](https://github.com/hanyscz/DeepSeekCreditCheck/releases)
2. Run `DeepSeekCreditCheck.UI.exe`
3. Right-click the tray icon → **⚙️ Settings** → enter your DeepSeek API key
4. Click **Save** — app restarts and begins polling
5. When a new version is released, you'll get a notification — click to update automatically

Your API key can be obtained at [platform.deepseek.com/api_keys](https://platform.deepseek.com/api_keys).

### Configuration

Open **⚙️ Settings** from the tray menu:

| Setting | Description |
|---------|-------------|
| API Key | Your DeepSeek API key (encrypted with Windows DPAPI) |
| Alert threshold | Balance below this triggers a notification (default $2.00) |
| Check interval | How often to poll the API (5–60 min) |
| Language | UI language — add your own in `Lang/*.json` |
| Log path | Custom log file location (leave empty for default) |
| DB path | Custom database location for sharing between PCs |
| Start with Windows | Register the app for automatic startup |

### Test notification

Settings window has an **🔔 Test notification** button that shows a sample low-balance alert in the custom toast popup.

### Test API key

Settings window has an **🔑 Otestovat klíč** button that immediately validates your API key. It shows the current balance on success, distinguishes an invalid key (HTTP 401) from a network error — no need to wait for the next poll cycle.

### Single instance

Only one instance of the app can run at a time. If you try to start it again, the second launch exits silently — preventing duplicate polling and tray icons.

### Data browser

Open **📁 Záznamy v DB** from the Dashboard to browse all balance history records. You can:
- View records sorted by time (newest first)
- Select multiple records (hold Ctrl) and delete them
- Export all records to a CSV file via the **📥 Export CSV** button

### Adding a language

1. Copy `Lang/en.json` → `Lang/fr.json`
2. Translate the values inside
3. Set the `"lang_name"` key to the display name (e.g. `"Français"`)
4. The new language appears automatically in Settings → Language

### 🏡 Home Assistant Integration

This repository includes a custom Home Assistant integration to monitor your DeepSeek balance and usage.

#### Features in Home Assistant:
- **Balance sensors** (USD/CNY) — total balance, topped-up balance, and promotional granted balance (via API Key).
- **Usage & cost sensors** — monthly and daily API cost, monthly and daily token counts with breakdown by Pro and Flash models in attributes (via Session Token).

#### Quick Setup:
1. Copy the `custom_components/deepseek_credit` folder into your Home Assistant `<config_dir>/custom_components/` directory.
2. Restart Home Assistant.
3. In Home Assistant, go to **Settings** -> **Devices & Services** -> **Add Integration** and search for **DeepSeek Credit Checker**.
4. Enter your **API Key** and/or **Session Token** (obtained from browser DevTools under `platform.deepseek.com` Network tab).

*(For details and custom repository installation via HACS, see the integration files in [custom_components/deepseek_credit](custom_components/deepseek_credit/)).*

---

🇨🇿 **Česky**

Odlehčená Windows aplikace běžící v systémové trayi, která pravidelně kontroluje zůstatek na DeepSeek API účtu. Zobrazuje zůstatek přímo na tray ikoně s barevným indikátorem, podrobnosti v tooltipu při najetí myší, předpovídá na jak dlouho kredit vydrží a upozorní při poklesu pod nastavenou mez.

### Funkce

- **🎨 Zůstatek na ikoně** — ikona dynamicky zobrazuje aktuální zůstatek jako číslo s barvou: zelená (OK), oranžová (blíží se prahu), červená (pod prahem), modrá (čeká na klíč)
- **🔥 Sledování tarifních špiček (Peak / Off-Peak)** — monitorování cenových pásem DeepSeek API v reálném čase (špička 01:00–04:00 a 06:00–10:00 UTC za plnou cenu, mimo špičku 50% sleva na všechny tokeny). Indikováno tečkou na tray ikoně (🔴 červená / 🟢 zelená), v tooltipu, v menu a na samostatném bloku dashboardu s odpočtem času
- **⚡ Analýza špiček v podrobných statistikách (Peak / Off-Peak)** — samostatná záložka pro detailní přehled a rozbor spotřeby ve špičce a mimo špičku, poměru nákladů, celkové finanční úspory z 50% slevy, grafu denního vývoje napříč dny v měsíci a přehledné tabulky podle modelů a pásem na základě přesných vyúčtovaných cen
- **💰 Zůstatek v trayi** — tooltip při najetí myší ukazuje zůstatek, dnešní spotřebu, predikci, tarif a čas
- **📋 Historie změn v aplikaci** — procházení novinek a přehledu verzí přímo v okně aplikace přes tlačítko na dashboardu, položku v tray menu nebo automaticky po dokončení aktualizace
- **📊 Dashboard** — graf hodinové spotřeby, dnešní spotřeba, průměr/den, statistiky za týden a měsíc a vyhrazený tarifní panel
- **📈 Predikce** — odhad zbývajících dní podle průměrné denní spotřeby z kalendářních dnů
- **⚠️ Vlastní notifikace** — tmavý toast v pravém dolním rohu s animací
- **🔒 Bezpečnost** — API klíč šifrovaný Windows DPAPI
- **🌐 Vícejazyčnost** — čeština a angličtina; vlastní jazyk přidáš přes JSON v `Lang/`
- **📁 Prohlížeč dat** — prohlížení a mazání historických záznamů s možností výběru více položek
- **📝 Logování** — chyby se zapisují do souboru; nastavitelná cesta pro synchronizaci mezi PC
- **🗄️ Sdílení databáze** — vlastní cesta k DB pro sdílení mezi počítači
- **🔄 Auto-update** — kontrola nových verzí při startu a každé 4 hodiny; stažení a instalace na jedno kliknutí z GitHub Releases
- **🚀 Spuštění při startu Windows** — volitelný autostart přes checkbox v Nastavení
- **💚 Detekce dobití** — při dobití kreditu se zobrazí pozitivní toast s novým zůstatkem
- **🔑 Integrace DeepSeek Platformy** — bezpečné přihlášení k platformě pomocí WebView2 a zobrazení podrobných měsíčních i denních statistik tokenů (Input Miss, Cache Hit, Output, Celkem) a přesných nákladů v USD zaokrouhlených na 2 desetinná místa, rozdělených podle modelů Pro, Flash a Vision přímo na dashboardu. Navigační šipky umožňují prohlížet historii spotřeby za předchozí měsíce a pro aktuální měsíc je zobrazen samostatný blok s denní spotřebou.
- **📊 Panel podrobných statistik** — stažení oficiálního ZIP exportu s CSV soubory na jedno kliknutí, jejich uložení do lokální SQLite databáze (stálá cache) a přehledná vizualizace denního trendu nákladů a meziměsíčního porovnání pomocí interaktivních grafů OxyPlot s moderními tmavými popisky (tooltips) v samostatném okně. Přehled API klíčů obsahuje sloupcový graf seřazený podle nákladů a seskupuje stejné klíče samostatně podle použitého modelu.
- **🌙 Tmavý režim** — všechna okna v moderním dark designu
- **🏡 Home Assistant Integrace** — monitorujte svůj DeepSeek zůstatek a detailní spotřebu přímo v Home Assistantovi (vlastní komponenta je součástí projektu)

### Použití

1. Stáhni build z [GitHub Releases](https://github.com/hanyscz/DeepSeekCreditCheck/releases)
2. Spusť `DeepSeekCreditCheck.UI.exe`
3. Klikni pravým na tray ikonu → **⚙️ Nastavení** → zadej DeepSeek API klíč
4. Klikni **Uložit** — aplikace se restartuje a začne kontrolovat zůstatek
5. Při vydání nové verze se zobrazí notifikace — klikni pro automatickou aktualizaci

API klíč získáš na [platform.deepseek.com/api_keys](https://platform.deepseek.com/api_keys).

### Nastavení

Otevři **⚙️ Nastavení** z tray menu:

| Nastavení | Popis |
|-----------|-------|
| API Klíč | Tvůj DeepSeek API klíč (šifrovaný DPAPI) |
| Práh upozornění | Zůstatek pod touto částkou spustí notifikaci (výchozí $2.00) |
| Interval kontroly | Jak často volat API (5–60 min) |
| Jazyk | Jazyk UI — vlastní přidáš do `Lang/*.json` |
| Cesta k logu | Vlastní umístění log souboru (nech prázdné pro výchozí) |
| Cesta k databázi | Vlastní umístění DB pro sdílení mezi PC |
| Spuštění při startu | Zaregistruje aplikaci pro automatický start Windows |

### Test notifikace

V Nastavení je tlačítko **🔔 Test notifikace**, které zobrazí ukázkovou nízkorozpočtovou výstrahu v custom toast okně.

### Otestovat klíč

V Nastavení je tlačítko **🔑 Otestovat klíč**, které okamžitě ověří platnost API klíče. Při úspěchu zobrazí aktuální zůstatek, rozlišuje neplatný klíč (HTTP 401) od síťové chyby — není nutné čekat na další plánovanou kontrolu.

### Jediná instance

Aplikaci lze spustit pouze jednou. Při druhém pokusu o spuštění se nová instance tiše ukončí — zabraňuje duplicitnímu pollingu a vícenásobným tray ikonám.

### Prohlížeč dat

Otevři **📁 Záznamy v DB** z Dashboardu. Můžeš:
- Prohlížet záznamy seřazené podle času (nejnovější první)
- Vybrat více záznamů (podrž Ctrl) a smazat je
- Exportovat všechny záznamy do CSV souboru tlačítkem **📥 Export CSV**

### Přidání jazyka

1. Zkopíruj `Lang/en.json` → `Lang/de.json`
2. Přelož hodnoty uvnitř
3. Nastav klíč `"lang_name"` na zobrazovaný název (např. `"Deutsch"`)
4. Nový jazyk se automaticky objeví v Nastavení → Jazyk

### 🏡 Home Assistant Integrace

Tento repozitář obsahuje také vlastní integraci do Home Assistanta pro sledování vašeho zůstatku a spotřeby DeepSeek.

#### Funkce v Home Assistantovi:
- **Senzory zůstatku** (USD/CNY) — celkový zůstatek, dobitý zůstatek a dárkový (promo) zůstatek (vyžaduje API klíč).
- **Senzory spotřeby a nákladů** — měsíční a denní náklady za volání API, celkový měsíční a denní počet tokenů s rozpadem na modely Pro a Flash v atributech (vyžaduje Session Token).

#### Rychlý návod:
1. Zkopírujte složku `custom_components/deepseek_credit` do složky `<config_dir>/custom_components/` ve vaší instalaci Home Assistanta.
2. Restartujte Home Assistanta.
3. Přejděte do **Nastavení** -> **Zařízení a služby** -> **Přidat integraci** a vyhledejte **DeepSeek Credit Checker**.
4. Zadejte svůj **API klíč** a/nebo **Session Token** (který získáte v prohlížeči v záložce Network při přihlášení na `platform.deepseek.com`).

*(Podrobnější návod a popis instalace jako vlastní repozitář v HACS naleznete přímo ve složce [custom_components/deepseek_credit](custom_components/deepseek_credit/)).*

---

## Build

```bash
dotnet publish src/DeepSeekCreditCheck.UI/DeepSeekCreditCheck.UI.csproj -c Release -r win-x64 --self-contained false -o publish/
```

Requires .NET 8 SDK and Windows (DPAPI dependency).

---

## Screenshots

<p align="center">
  <img src="screenshots/tray.png" alt="Tray icon" width="200" />
  <img src="screenshots/popup.png" alt="Notification toast" width="200" />
  <br/>
  <img src="screenshots/dashboard.png" alt="Dashboard" width="400" />
  <img src="screenshots/stats_01.png" alt="Dashboard" width="400" />
  <img src="screenshots/stats_02.png" alt="Dashboard" width="400" />
  <img src="screenshots/stats_03.png" alt="Dashboard" width="400" />
  <img src="screenshots/stats_04.png" alt="Dashboard" width="400" />
</p>
