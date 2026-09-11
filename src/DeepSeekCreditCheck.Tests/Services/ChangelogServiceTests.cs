using DeepSeekCreditCheck.Core.Services;
using Xunit;

namespace DeepSeekCreditCheck.Tests.Services;

public class ChangelogServiceTests
{
    private readonly ChangelogService _service = new();

    [Fact]
    public void GetChangelogText_Czech_ReturnsContent()
    {
        var text = _service.GetChangelogText("cs");

        Assert.NotNull(text);
        Assert.NotEmpty(text);
        Assert.Contains("v1.12.0", text);
        Assert.Contains("v1.11.0", text);
        Assert.Contains("v1.10.0", text);
    }

    [Fact]
    public void GetChangelogText_English_ReturnsContent()
    {
        var text = _service.GetChangelogText("en");

        Assert.NotNull(text);
        Assert.NotEmpty(text);
        Assert.Contains("v1.12.0", text);
        Assert.Contains("v1.11.0", text);
        Assert.Contains("v1.10.0", text);
    }

    [Fact]
    public void GetVersions_ReturnsParsedVersions()
    {
        var versions = _service.GetVersions("cs");

        Assert.NotNull(versions);
        Assert.NotEmpty(versions);

        var v1120 = versions.FirstOrDefault(v => v.Version.Contains("1.12.0"));
        Assert.NotNull(v1120);
        Assert.Equal("2026-09-11", v1120.Date);
        Assert.NotEmpty(v1120.Content);

        var v1110 = versions.FirstOrDefault(v => v.Version.Contains("1.11.0"));
        Assert.NotNull(v1110);
        Assert.Equal("2026-09-06", v1110.Date);
        Assert.NotEmpty(v1110.Content);

        var v1100 = versions.FirstOrDefault(v => v.Version.Contains("1.10.0"));
        Assert.NotNull(v1100);
        Assert.Equal("2026-08-18", v1100.Date);
        Assert.NotEmpty(v1100.Content);
    }
}
