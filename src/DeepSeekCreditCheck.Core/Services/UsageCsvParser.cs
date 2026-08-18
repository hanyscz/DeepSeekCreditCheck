using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public static class UsageCsvParser
{
    /// <summary>
    /// Naparsuje ZIP archiv se statistikami platformy DeepSeek.
    /// Hledá soubor s názvem obsahujícím 'amount' a příponou '.csv'.
    /// </summary>
    public static List<UsageDetailSnapshot> ParseZip(byte[] zipBytes, int year = 0, int month = 0)
    {
        if (zipBytes == null || zipBytes.Length == 0)
        {
            throw new ArgumentException("ZIP archiv je prázdný.", nameof(zipBytes));
        }

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        ZipArchiveEntry? amountCsvEntry = null;
        foreach (var entry in archive.Entries)
        {
            if (entry.FullName.Contains("amount", StringComparison.OrdinalIgnoreCase) && 
                entry.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                amountCsvEntry = entry;
                break;
            }
        }

        if (amountCsvEntry == null)
        {
            throw new FileNotFoundException("V ZIP archivu nebyl nalezen CSV soubor s množstvím tokenů (amount).");
        }

        using var stream = amountCsvEntry.Open();
        return ParseCsvStream(stream, year, month);
    }

    /// <summary>
    /// Naparsuje CSV data ze Streamu (např. přímo z dekomprimovaného souboru nebo z disku).
    /// </summary>
    public static List<UsageDetailSnapshot> ParseCsvStream(Stream stream, int defaultYear = 0, int defaultMonth = 0)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return ParseCsvReader(reader, defaultYear, defaultMonth);
    }

    /// <summary>
    /// Naparsuje CSV data z textového řetězce.
    /// </summary>
    public static List<UsageDetailSnapshot> ParseCsv(string csvContent, int defaultYear = 0, int defaultMonth = 0)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            return new List<UsageDetailSnapshot>();
        }

        using var reader = new StringReader(csvContent);
        return ParseCsvReader(reader, defaultYear, defaultMonth);
    }

    private static List<UsageDetailSnapshot> ParseCsvReader(TextReader reader, int defaultYear, int defaultMonth)
    {
        var list = new List<UsageDetailSnapshot>();

        var header = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(header))
        {
            return list;
        }

        var columns = SplitCsvLine(header);

        // Flexibilní vyhledání indexů sloupců (case-insensitive)
        int idxDate = FindColumnIndex(columns, "start_time_iso", "start_time", "utc_date", "date", "timestamp");
        int idxModel = FindColumnIndex(columns, "model");
        int idxApiKeyName = FindColumnIndex(columns, "api_key_name", "apikey_name", "key_name");
        int idxApiKeyMasked = FindColumnIndex(columns, "api_key", "apikey", "api_key_masked", "key");
        int idxType = FindColumnIndex(columns, "type", "token_type", "usage_type");
        int idxPrice = FindColumnIndex(columns, "price", "unit_price", "cost_per_token");
        int idxAmount = FindColumnIndex(columns, "amount", "count", "tokens");

        if (idxDate == -1 || idxModel == -1 || idxApiKeyName == -1 || 
            idxApiKeyMasked == -1 || idxType == -1 || idxAmount == -1)
        {
            throw new FormatException("CSV soubor má neplatnou hlavičku. Chybí povinné sloupce (datum, model, api_key_name, api_key, type, amount).");
        }

        int maxIndex = Math.Max(idxDate, Math.Max(idxModel, Math.Max(idxApiKeyName, Math.Max(idxApiKeyMasked, Math.Max(idxType, idxAmount)))));
        if (idxPrice != -1)
        {
            maxIndex = Math.Max(maxIndex, idxPrice);
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = SplitCsvLine(line);
            if (values.Count <= maxIndex)
            {
                continue; // neúplný řádek
            }

            var rawDate = values[idxDate];
            var formattedDate = ExtractDate(rawDate);

            // Pokud nebyl předán rok/měsíc z volání, odvodíme ho přímo z parsovaného data řádku
            int itemYear = defaultYear;
            int itemMonth = defaultMonth;
            if (itemYear <= 0 || itemMonth <= 0)
            {
                if (DateTime.TryParseExact(formattedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDt))
                {
                    itemYear = parsedDt.Year;
                    itemMonth = parsedDt.Month;
                }
            }

            // Parsování ceny (nepovinné, pro request_count může chybět)
            double? parsedPrice = null;
            if (idxPrice != -1 && idxPrice < values.Count && !string.IsNullOrWhiteSpace(values[idxPrice]))
            {
                if (double.TryParse(values[idxPrice], NumberStyles.Any, CultureInfo.InvariantCulture, out var prc))
                {
                    parsedPrice = prc;
                }
            }

            bool isPeak = TariffService.DetermineIsPeak(values[idxModel], values[idxType], parsedPrice, rawDate);

            var snapshot = new UsageDetailSnapshot
            {
                Year = itemYear,
                Month = itemMonth,
                UtcDate = formattedDate,
                StartTimeIso = rawDate,
                IsPeak = isPeak,
                Model = values[idxModel],
                ApiKeyName = values[idxApiKeyName],
                ApiKeyMasked = values[idxApiKeyMasked],
                Type = values[idxType],
                Price = parsedPrice
            };

            // Parsování množství
            if (long.TryParse(values[idxAmount], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amt))
            {
                snapshot.Amount = amt;
            }
            else if (double.TryParse(values[idxAmount], NumberStyles.Any, CultureInfo.InvariantCulture, out var amtDbl))
            {
                snapshot.Amount = (long)Math.Round(amtDbl);
            }

            list.Add(snapshot);
        }

        return list;
    }

    /// <summary>
    /// Převede ISO 8601 řetězec nebo datum na standardní formát yyyy-MM-dd.
    /// </summary>
    public static string ExtractDate(string rawDate)
    {
        if (string.IsNullOrWhiteSpace(rawDate)) return "";

        rawDate = rawDate.Trim();

        // 1. Zkusit ISO 8601 s časem i posunem zóny (např. 2026-08-17T15:00:00+02:00)
        if (DateTimeOffset.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
        {
            return dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // 2. Standardní datum (např. 2026-08-17)
        if (DateTime.TryParse(rawDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // 3. Fallback: Pokud řetězec začíná formátem yyyy-MM-dd
        if (rawDate.Length >= 10 && rawDate[4] == '-' && rawDate[7] == '-')
        {
            return rawDate.Substring(0, 10);
        }

        return rawDate;
    }

    private static int FindColumnIndex(List<string> columns, params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            int index = columns.FindIndex(c => string.Equals(c, candidate, StringComparison.OrdinalIgnoreCase));
            if (index != -1)
            {
                return index;
            }
        }
        return -1;
    }

    public static List<string> SplitCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var current = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString().Trim());
        return result;
    }
}
