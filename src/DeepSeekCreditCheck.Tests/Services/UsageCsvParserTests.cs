using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using DeepSeekCreditCheck.Core.Services;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class UsageCsvParserTests
{
    private byte[] CreateTestZip(string csvFileName, string csvContent)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = archive.CreateEntry(csvFileName);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, Encoding.UTF8);
            writer.Write(csvContent);
        }
        return ms.ToArray();
    }

    [Fact]
    public void SplitCsvLine_NormalValues_ReturnsSplitValues()
    {
        var line = "val1,val2,val3";
        var result = UsageCsvParser.SplitCsvLine(line);
        Assert.Equal(3, result.Count);
        Assert.Equal("val1", result[0]);
        Assert.Equal("val2", result[1]);
        Assert.Equal("val3", result[2]);
    }

    [Fact]
    public void SplitCsvLine_QuotedCommas_ReturnsSplitValuesWithoutBreakingQuotes()
    {
        var line = "val1,\"val2, with comma\",val3";
        var result = UsageCsvParser.SplitCsvLine(line);
        Assert.Equal(3, result.Count);
        Assert.Equal("val1", result[0]);
        Assert.Equal("val2, with comma", result[1]);
        Assert.Equal("val3", result[2]);
    }

    [Fact]
    public void ParseZip_LegacyFormatWithUtcDate_ReturnsParsedSnapshots()
    {
        // Arrange
        var csvContent = "user_id,utc_date,model,api_key_name,api_key,type,price,amount\n" +
                         "user123,2026-06-01,deepseek-v4-pro,\"VS Code, Copilot\",sk-key1,input_cache_hit_tokens,0.000000003625,8275456\n" +
                         "user123,2026-06-01,deepseek-v4-pro,\"VS Code, Copilot\",sk-key1,request_count,,92";

        var zipBytes = CreateTestZip("usage/amount-2026-6.csv", csvContent);

        // Act
        var result = UsageCsvParser.ParseZip(zipBytes, 2026, 6);

        // Assert
        Assert.Equal(2, result.Count);
        
        var first = result[0];
        Assert.Equal(2026, first.Year);
        Assert.Equal(6, first.Month);
        Assert.Equal("2026-06-01", first.UtcDate);
        Assert.Equal("deepseek-v4-pro", first.Model);
        Assert.Equal("VS Code, Copilot", first.ApiKeyName);
        Assert.Equal("sk-key1", first.ApiKeyMasked);
        Assert.Equal("input_cache_hit_tokens", first.Type);
        Assert.Equal(0.000000003625, first.Price);
        Assert.Equal(8275456, first.Amount);

        var second = result[1];
        Assert.Equal("request_count", second.Type);
        Assert.Null(second.Price);
        Assert.Equal(92, second.Amount);
    }

    [Fact]
    public void ParseZip_NewFormatWithStartTimeIso_ReturnsParsedSnapshots()
    {
        // Arrange
        var csvContent = "user_id,start_time_iso,end_time_iso,model,api_key_name,api_key,type,price,amount\n" +
                         "93fe3f7b-eac7-4030-a6b8-be7eea2ea0ca,2026-08-17T15:00:00+02:00,2026-08-17T16:00:00+02:00,deepseek-v4-flash,Account Balance,sk-b606a***********************0989,input_cache_hit_tokens,0.000000007,210688\n" +
                         "93fe3f7b-eac7-4030-a6b8-be7eea2ea0ca,2026-08-17T15:00:00+02:00,2026-08-17T16:00:00+02:00,deepseek-v4-flash,Account Balance,sk-b606a***********************0989,request_count,,10\n" +
                         "93fe3f7b-eac7-4030-a6b8-be7eea2ea0ca,2026-08-17T15:00:00+02:00,2026-08-17T16:00:00+02:00,deepseek-v4-flash,Account Balance,sk-b606a***********************0989,output_tokens,0.00000066,4373";

        var zipBytes = CreateTestZip("amount-2026-08-17_2026-08-17.csv", csvContent);

        // Act
        var result = UsageCsvParser.ParseZip(zipBytes);

        // Assert
        Assert.Equal(3, result.Count);

        var first = result[0];
        Assert.Equal(2026, first.Year);
        Assert.Equal(8, first.Month);
        Assert.Equal("2026-08-17", first.UtcDate);
        Assert.Equal("deepseek-v4-flash", first.Model);
        Assert.Equal("Account Balance", first.ApiKeyName);
        Assert.Equal("sk-b606a***********************0989", first.ApiKeyMasked);
        Assert.Equal("input_cache_hit_tokens", first.Type);
        Assert.Equal(0.000000007, first.Price);
        Assert.Equal(210688, first.Amount);

        var second = result[1];
        Assert.Equal("request_count", second.Type);
        Assert.Null(second.Price);
        Assert.Equal(10, second.Amount);

        var third = result[2];
        Assert.Equal("output_tokens", third.Type);
        Assert.Equal(0.00000066, third.Price);
        Assert.Equal(4373, third.Amount);
    }

    [Fact]
    public void ParseZip_RealExportZipFromDocs_SuccessfullyParsesAllRecords()
    {
        // Arrange
        var projectRoot = AppContext.BaseDirectory;
        // Vyhledat složku docs směrem nahoru
        var dir = new DirectoryInfo(projectRoot);
        string? zipPath = null;
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "docs", "usage_data_2026-08-17_2026-08-17.zip");
            if (File.Exists(candidate))
            {
                zipPath = candidate;
                break;
            }
            dir = dir.Parent;
        }

        if (zipPath != null && File.Exists(zipPath))
        {
            var zipBytes = File.ReadAllBytes(zipPath);

            // Act
            var result = UsageCsvParser.ParseZip(zipBytes);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(4, result.Count);
            Assert.All(result, r =>
            {
                Assert.Equal(2026, r.Year);
                Assert.Equal(8, r.Month);
                Assert.Equal("2026-08-17", r.UtcDate);
                Assert.Equal("deepseek-v4-flash", r.Model);
            });
        }
    }

    [Fact]
    public void ParseCsv_DirectString_ReturnsParsedSnapshots()
    {
        var csvContent = "user_id,start_time_iso,end_time_iso,model,api_key_name,api_key,type,price,amount\n" +
                         "user1,2026-08-17T15:00:00+02:00,2026-08-17T16:00:00+02:00,deepseek-chat,Key1,sk-xxx,output_tokens,0.000002,500";

        var result = UsageCsvParser.ParseCsv(csvContent);

        Assert.Single(result);
        Assert.Equal("2026-08-17", result[0].UtcDate);
        Assert.Equal(2026, result[0].Year);
        Assert.Equal(8, result[0].Month);
        Assert.Equal(500, result[0].Amount);
    }

    [Fact]
    public void ExtractDate_VariousFormats_ReturnsNormalizedDate()
    {
        Assert.Equal("2026-08-17", UsageCsvParser.ExtractDate("2026-08-17T15:00:00+02:00"));
        Assert.Equal("2026-08-17", UsageCsvParser.ExtractDate("2026-08-17T13:00:00Z"));
        Assert.Equal("2026-08-17", UsageCsvParser.ExtractDate("2026-08-17"));
        Assert.Equal("2026-06-01", UsageCsvParser.ExtractDate("2026-06-01"));
        Assert.Equal("", UsageCsvParser.ExtractDate(""));
    }

    [Fact]
    public void ParseZip_MissingAmountCsv_ThrowsFileNotFoundException()
    {
        // Arrange
        var zipBytes = CreateTestZip("usage/other-file.csv", "dummy,content");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => UsageCsvParser.ParseZip(zipBytes, 2026, 6));
    }

    [Fact]
    public void ParseZip_InvalidHeader_ThrowsFormatException()
    {
        // Arrange
        var csvContent = "user_id,start_time_iso,wrong_column,api_key_name,api_key,type,price,amount\n" +
                         "user123,2026-06-01,deepseek-v4-pro,VS Code,sk-key1,input_cache_hit_tokens,0.000000003625,8275456";

        var zipBytes = CreateTestZip("usage/amount-2026-6.csv", csvContent);

        // Act & Assert
        Assert.Throws<FormatException>(() => UsageCsvParser.ParseZip(zipBytes, 2026, 6));
    }

    [Fact]
    public void ParseCsv_WithPeakAndOffPeakIsoDates_SetsIsPeakCorrectly()
    {
        // 02:00 UTC je Peak (špičkové okno 01:00-04:00 UTC)
        // 13:00 UTC (15:00+02:00) je Off-Peak (mimo špičku)
        var csv = "user_id,start_time_iso,end_time_iso,model,api_key_name,api_key,type,price,amount\n" +
                  "u1,2026-08-17T02:00:00Z,2026-08-17T03:00:00Z,deepseek-v4-flash,Key1,sk-1,input_cache_hit_tokens,0.000000014,1000\n" +
                  "u1,2026-08-17T15:00:00+02:00,2026-08-17T16:00:00+02:00,deepseek-v4-flash,Key1,sk-1,input_cache_hit_tokens,0.000000007,2000";

        var list = UsageCsvParser.ParseCsv(csv, 2026, 8);

        Assert.Equal(2, list.Count);
        Assert.True(list[0].IsPeak, "02:00 UTC má být Peak");
        Assert.Equal("2026-08-17T02:00:00Z", list[0].StartTimeIso);

        Assert.False(list[1].IsPeak, "15:00+02:00 (13:00 UTC) má být Off-Peak");
        Assert.Equal("2026-08-17T15:00:00+02:00", list[1].StartTimeIso);
    }
}
