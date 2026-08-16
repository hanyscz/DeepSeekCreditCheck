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
        Assert.Contains("v1.9.0", text);
    }

    [Fact]
    public void GetChangelogText_English_ReturnsContent()
    {
        var text = _service.GetChangelogText("en");

        Assert.NotNull(text);
        Assert.NotEmpty(text);
        Assert.Contains("v1.9.0", text);
    }

    [Fact]
    public void GetVersions_ReturnsParsedVersions()
    {
        var versions = _service.GetVersions("cs");

        Assert.NotNull(versions);
        Assert.NotEmpty(versions);

        var v190 = versions.FirstOrDefault(v => v.Version.Contains("1.9.0"));
        Assert.NotNull(v190);
        Assert.Equal("2026-08-16", v190.Date);
        Assert.NotEmpty(v190.Content);
    }
}
