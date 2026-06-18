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
    public static List<UsageDetailSnapshot> ParseZip(byte[] zipBytes, int year, int month)
    {
        var list = new List<UsageDetailSnapshot>();

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
        using var reader = new StreamReader(stream, Encoding.UTF8);

        var header = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(header))
        {
            return list;
        }

        var columns = SplitCsvLine(header);
        int idxUtcDate = columns.IndexOf("utc_date");
        int idxModel = columns.IndexOf("model");
        int idxApiKeyName = columns.IndexOf("api_key_name");
        int idxApiKeyMasked = columns.IndexOf("api_key");
        int idxType = columns.IndexOf("type");
        int idxPrice = columns.IndexOf("price");
        int idxAmount = columns.IndexOf("amount");

        if (idxUtcDate == -1 || idxModel == -1 || idxApiKeyName == -1 || 
            idxApiKeyMasked == -1 || idxType == -1 || idxAmount == -1)
        {
            throw new FormatException("CSV soubor má neplatnou hlavičku. Chybí povinné sloupce.");
        }

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var values = SplitCsvLine(line);
            if (values.Count <= Math.Max(idxUtcDate, Math.Max(idxModel, Math.Max(idxApiKeyName, Math.Max(idxApiKeyMasked, Math.Max(idxType, idxAmount))))))
            {
                continue; // neúplný řádek
            }

            var snapshot = new UsageDetailSnapshot
            {
                Year = year,
                Month = month,
                UtcDate = values[idxUtcDate],
                Model = values[idxModel],
                ApiKeyName = values[idxApiKeyName],
                ApiKeyMasked = values[idxApiKeyMasked],
                Type = values[idxType]
            };

            // Parsování množství
            if (long.TryParse(values[idxAmount], NumberStyles.Integer, CultureInfo.InvariantCulture, out var amt))
            {
                snapshot.Amount = amt;
            }

            // Parsování ceny (nepovinné, pro request_count může chybět)
            if (idxPrice != -1 && idxPrice < values.Count && !string.IsNullOrWhiteSpace(values[idxPrice]))
            {
                if (double.TryParse(values[idxPrice], NumberStyles.Any, CultureInfo.InvariantCulture, out var prc))
                {
                    snapshot.Price = prc;
                }
            }

            list.Add(snapshot);
        }

        return list;
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
