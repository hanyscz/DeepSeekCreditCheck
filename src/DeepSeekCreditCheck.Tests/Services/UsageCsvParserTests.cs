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
    public void ParseZip_ValidZipAndCsv_ReturnsParsedSnapshots()
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
        var csvContent = "user_id,utc_date,wrong_column,api_key_name,api_key,type,price,amount\n" +
                         "user123,2026-06-01,deepseek-v4-pro,VS Code,sk-key1,input_cache_hit_tokens,0.000000003625,8275456";

        var zipBytes = CreateTestZip("usage/amount-2026-6.csv", csvContent);

        // Act & Assert
        Assert.Throws<FormatException>(() => UsageCsvParser.ParseZip(zipBytes, 2026, 6));
    }
}
