# DeepSeek Credit Checker

**Monitor your DeepSeek API credit balance from the Windows system tray.**

---

🇬🇧 **English**

A lightweight Windows application that runs in the system tray and periodically checks your DeepSeek API account balance. Shows a static tray icon, displays your balance and prediction on hover, and alerts you when balance drops below a configurable threshold.

### Features

- **💰 Balance in tray** — hover tooltip shows current balance, prediction, and last update time
- **📊 Dashboard** — hourly spend chart with calendar-day aggregation, weekly/monthly spend stats
- **📈 Prediction** — estimates remaining days based on average daily spend per calendar day
- **⚠️ Custom notification toast** — dark-themed popup in bottom-right corner with fade-in animation
- **🔒 Secure** — API key encrypted with Windows DPAPI
- **🌐 Multi-language** — Czech and English built-in; add your own via JSON files in `Lang/`
- **📁 Data browser** — view and delete historical balance records with multi-select
- **📝 Logging** — errors logged to file; configurable log path for multi-PC sync
- **🗄️ Configurable DB path** — share database across PCs via network drive or cloud sync
- **🌙 Dark theme** — all windows with modern dark design

### How to use

1. Download the latest build from `publish/`
2. Run `DeepSeekCreditCheck.UI.exe`
3. Right-click the tray icon → **⚙️ Settings** → enter your DeepSeek API key
4. Click **Save** — app restarts and begins polling

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

### Test notification

Settings window has an **🔔 Test notification** button that shows a sample low-balance alert in the custom toast popup.

### Data browser

Open **📁 Záznamy v DB** from the Dashboard to browse all balance history records. You can:
- View records sorted by time (newest first)
- Select multiple records (hold Ctrl) and delete them

### Adding a language

1. Copy `Lang/en.json` → `Lang/fr.json`
2. Translate the values inside
3. Set the `"lang_name"` key to the display name (e.g. `"Français"`)
4. The new language appears automatically in Settings → Language

---

🇨🇿 **Česky**

Odlehčená Windows aplikace běžící v systémové trayi, která pravidelně kontroluje zůstatek na DeepSeek API účtu. Zobrazuje informace v tooltipu při najetí myší, předpovídá na jak dlouho kredit vydrží a upozorní při poklesu pod nastavenou mez.

### Funkce

- **💰 Zůstatek v trayi** — tooltip při najetí myší ukazuje zůstatek, predikci a čas
- **📊 Dashboard** — graf hodinové spotřeby, průměr/den, statistiky za týden a měsíc
- **📈 Predikce** — odhad zbývajících dní podle průměrné denní spotřeby z kalendářních dnů
- **⚠️ Vlastní notifikace** — tmavý toast v pravém dolním rohu s animací
- **🔒 Bezpečnost** — API klíč šifrovaný Windows DPAPI
- **🌐 Vícejazyčnost** — čeština a angličtina; vlastní jazyk přidáš přes JSON v `Lang/`
- **📁 Prohlížeč dat** — prohlížení a mazání historických záznamů s možností výběru více položek
- **📝 Logování** — chyby se zapisují do souboru; nastavitelná cesta pro synchronizaci mezi PC
- **🗄️ Sdílení databáze** — vlastní cesta k DB pro sdílení mezi počítači
- **🌙 Tmavý režim** — všechna okna v moderním dark designu

### Použití

1. Stáhni build ze složky `publish/`
2. Spusť `DeepSeekCreditCheck.UI.exe`
3. Klikni pravým na tray ikonu → **⚙️ Nastavení** → zadej DeepSeek API klíč
4. Klikni **Uložit** — aplikace se restartuje a začne kontrolovat zůstatek

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

### Test notifikace

V Nastavení je tlačítko **🔔 Test notifikace**, které zobrazí ukázkovou nízkorozpočtovou výstrahu v custom toast okně.

### Prohlížeč dat

Otevři **📁 Záznamy v DB** z Dashboardu. Můžeš:
- Prohlížet záznamy seřazené podle času (nejnovější první)
- Vybrat více záznamů (podrž Ctrl) a smazat je

### Přidání jazyka

1. Zkopíruj `Lang/en.json` → `Lang/de.json`
2. Přelož hodnoty uvnitř
3. Nastav klíč `"lang_name"` na zobrazovaný název (např. `"Deutsch"`)
4. Nový jazyk se automaticky objeví v Nastavení → Jazyk

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
  <img src="screenshots/popup.png" alt="Notification toast" width="380" />
  <br/>
  <img src="screenshots/dashboard.png" alt="Dashboard" width="800" />
</p>
