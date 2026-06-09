using System.IO.Compression;
using System.Net;
using System.Text.Json;
using Moq;
using Moq.Protected;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

/// <summary>
/// Test version of UpdateService that pins CurrentVersion to 1.0.0
/// and redirects the updates directory to a test temp folder.
/// </summary>
public class TestUpdateService : UpdateService
{
    private readonly string _testUpdatesDir;

    public TestUpdateService(HttpClient httpClient, string testDir) : base(httpClient)
    {
        _testUpdatesDir = Path.Combine(testDir, "updates");
    }

    protected override Version ReadCurrentVersion() => new(1, 0, 0);

    protected override string GetUpdatesDir() => _testUpdatesDir;
}

public class UpdateServiceTests : IDisposable
{
    private readonly string _testDir;

    public UpdateServiceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"DeepSeekCreditCheck_Tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        Directory.CreateDirectory(Path.Combine(_testDir, "updates"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, recursive: true); } catch { }
    }

    // ──────────────────────────────────
    // Helpers
    // ──────────────────────────────────

    private static Mock<HttpMessageHandler> CreateHandler(HttpStatusCode status, string json)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json)
            });
        return handler;
    }

    private static Mock<HttpMessageHandler> CreateStreamHandler(byte[] content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new ByteArrayContent(content)
            });
        return handler;
    }

    private static GithubRelease MakeRelease(string tag, string? assetName = null)
    {
        var assets = new List<GithubReleaseAsset>();
        if (assetName != null)
        {
            assets.Add(new GithubReleaseAsset
            {
                Name = assetName,
                DownloadUrl = "https://example.com/release.zip",
                Size = 1024
            });
        }
        return new GithubRelease
        {
            TagName = tag,
            HtmlUrl = $"https://github.com/hanyscz/DeepSeekCreditCheck/releases/tag/{tag}",
            Assets = assets
        };
    }

    private static string ReleaseJson(string tagName, string? assetName = null)
    {
        var assets = assetName != null
            ? $@"[{{ ""name"": ""{assetName}"", ""browser_download_url"": ""https://example.com/release.zip"", ""size"": 1024 }}]"
            : "[]";

        return $@"{{
            ""tag_name"": ""{tagName}"",
            ""html_url"": ""https://github.com/repo/releases/tag/{tagName}"",
            ""body"": ""Release notes"",
            ""prerelease"": false,
            ""assets"": {assets}
        }}";
    }

    private static byte[] CreateTestZip()
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("test.txt");
            using var writer = new StreamWriter(entry.Open());
            writer.Write("hello from update");
        }
        return ms.ToArray();
    }

    private TestUpdateService CreateService(HttpMessageHandler handler)
        => new(new HttpClient(handler), _testDir);

    // ──────────────────────────────────
    // CurrentVersion
    // ──────────────────────────────────

    [Fact]
    public void CurrentVersion_IsPinnedTo_1_0_0()
    {
        var service = CreateService(new HttpClientHandler());
        Assert.Equal(new Version(1, 0, 0), service.CurrentVersion);
    }

    // ──────────────────────────────────
    // CheckForUpdateAsync
    // ──────────────────────────────────

    [Fact]
    public async Task CheckForUpdate_NewerVersion_ReturnsRelease()
    {
        var json = ReleaseJson("v1.5.0");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("v1.5.0", result.TagName);
        Assert.Equal(new Version(1, 5, 0), result.Version);
    }

    [Fact]
    public async Task CheckForUpdate_SameVersion_ReturnsNull()
    {
        var json = ReleaseJson("v1.0.0");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdate_OlderVersion_ReturnsNull()
    {
        var json = ReleaseJson("v0.9.0");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task CheckForUpdate_TagWithoutVPrefix_StillParses()
    {
        var json = ReleaseJson("2.0.0");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(new Version(2, 0, 0), result.Version);
    }

    [Fact]
    public async Task CheckForUpdate_ApiReturns404_Throws()
    {
        var handler = CreateHandler(HttpStatusCode.NotFound, "not found");
        var service = CreateService(handler.Object);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CheckForUpdateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CheckForUpdate_ApiReturns403_Throws()
    {
        var handler = CreateHandler(HttpStatusCode.Forbidden, "rate limited");
        var service = CreateService(handler.Object);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.CheckForUpdateAsync(CancellationToken.None));
    }

    [Fact]
    public async Task CheckForUpdate_NoVersionInTag_ReturnsNull()
    {
        var json = ReleaseJson("not-a-version");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.Null(result);
    }

    // ──────────────────────────────────
    // DownloadAndExtractAsync
    // ──────────────────────────────────

    [Fact]
    public async Task DownloadAndExtract_ValidZip_SetsPendingState()
    {
        var zipBytes = CreateTestZip();
        var handler = CreateStreamHandler(zipBytes);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0", "DeepSeekCreditCheck-v2.0.0.zip");

        await service.DownloadAndExtractAsync(release, null, CancellationToken.None);

        Assert.True(service.IsUpdatePending);
        Assert.NotNull(service.PendingRelease);
        Assert.Equal("v2.0.0", service.PendingRelease!.TagName);

        var extractDir = Path.Combine(_testDir, "updates", "v2.0.0");
        Assert.True(Directory.Exists(extractDir));
        Assert.True(File.Exists(Path.Combine(extractDir, "test.txt")));
    }

    [Fact]
    public async Task DownloadAndExtract_ReportsProgress()
    {
        var zipBytes = CreateTestZip();
        var handler = CreateStreamHandler(zipBytes);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0", "DeepSeekCreditCheck-v2.0.0.zip");

        var progressValues = new List<double>();
        var progress = new Progress<double>(v => progressValues.Add(v));

        await service.DownloadAndExtractAsync(release, progress, CancellationToken.None);

        Assert.NotEmpty(progressValues);
        Assert.Equal(1.0, progressValues.Last(), precision: 2);
    }

    [Fact]
    public async Task DownloadAndExtract_NoZipAsset_Throws()
    {
        var handler = CreateStreamHandler(new byte[0]);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0");
        release.Assets.Clear();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DownloadAndExtractAsync(release, null, CancellationToken.None));

        Assert.Contains("No zip asset", ex.Message);
    }

    [Fact]
    public async Task DownloadAndExtract_ExistingExtractDir_Overwrites()
    {
        var extractDir = Path.Combine(_testDir, "updates", "v2.0.0");
        Directory.CreateDirectory(extractDir);
        File.WriteAllText(Path.Combine(extractDir, "old_file.dll"), "stale");

        var zipBytes = CreateTestZip();
        var handler = CreateStreamHandler(zipBytes);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0", "DeepSeekCreditCheck-v2.0.0.zip");

        await service.DownloadAndExtractAsync(release, null, CancellationToken.None);

        Assert.False(File.Exists(Path.Combine(extractDir, "old_file.dll")));
        Assert.True(File.Exists(Path.Combine(extractDir, "test.txt")));
    }

    [Fact]
    public async Task DownloadAndExtract_ZipFileCleanedUp()
    {
        var zipBytes = CreateTestZip();
        var handler = CreateStreamHandler(zipBytes);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0", "DeepSeekCreditCheck-v2.0.0.zip");

        await service.DownloadAndExtractAsync(release, null, CancellationToken.None);

        var zipPath = Path.Combine(_testDir, "updates", "v2.0.0.zip");
        Assert.False(File.Exists(zipPath));
        Assert.False(File.Exists(zipPath + ".tmp"));
    }

    // ──────────────────────────────────
    // PrepareUpdateScriptAsync
    // ──────────────────────────────────

    [Fact]
    public async Task PrepareUpdateScript_CreatesScriptWithCorrectPaths()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0");

        var scriptPath = await service.PrepareUpdateScriptAsync(release);

        Assert.True(File.Exists(scriptPath));
        var content = await File.ReadAllTextAsync(scriptPath);
        Assert.Contains("xcopy", content);
        Assert.Contains("DeepSeekCreditCheck.UI.exe", content);
        Assert.Contains("v2.0.0", content);
    }

    [Fact]
    public async Task PrepareUpdateScript_WritesSuccessMarker()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0");

        await service.PrepareUpdateScriptAsync(release);

        var markerPath = Path.Combine(_testDir, "updates", "update_success.txt");
        Assert.True(File.Exists(markerPath));
        Assert.Equal("v2.0.0", (await File.ReadAllTextAsync(markerPath)).Trim());
    }

    // ──────────────────────────────────
    // MarkUpdateAvailable
    // ──────────────────────────────────

    [Fact]
    public void MarkUpdateAvailable_SetsState()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0");

        service.MarkUpdateAvailable(release);

        Assert.True(service.IsUpdatePending);
        Assert.Same(release, service.PendingRelease);
    }

    [Fact]
    public void MarkUpdateAvailable_FiresEvent()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0");
        GithubRelease? received = null;
        service.UpdateAvailable += r => received = r;

        service.MarkUpdateAvailable(release);

        Assert.Same(release, received);
    }

    // ──────────────────────────────────
    // WriteSuccessMarker / ConsumeSuccessMarker
    // ──────────────────────────────────

    [Fact]
    public void ConsumeMarker_NoFile_ReturnsNull()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);

        var result = service.ConsumeSuccessMarker();

        Assert.Null(result);
    }

    [Fact]
    public void WriteThenConsume_ReturnsVersionAndDeletesFile()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);

        service.WriteSuccessMarker("v4.5.6");
        var result = service.ConsumeSuccessMarker();

        Assert.Equal("v4.5.6", result);

        var markerPath = Path.Combine(_testDir, "updates", "update_success.txt");
        Assert.False(File.Exists(markerPath));
    }

    // ──────────────────────────────────
    // DownloadAndApplyAsync
    // ──────────────────────────────────

    [Fact]
    public async Task DownloadAndApply_NoPendingRelease_Throws()
    {
        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DownloadAndApplyAsync(null, CancellationToken.None));

        Assert.Contains("No pending update", ex.Message);
    }

    [Fact]
    public async Task DownloadAndApply_WithPendingRelease_Works()
    {
        var zipBytes = CreateTestZip();
        var handler = CreateStreamHandler(zipBytes);
        var service = CreateService(handler.Object);
        var release = MakeRelease("v2.0.0", "DeepSeekCreditCheck-v2.0.0.zip");
        service.MarkUpdateAvailable(release);

        var scriptPath = await service.DownloadAndApplyAsync(null, CancellationToken.None);

        Assert.True(File.Exists(scriptPath));
        Assert.Contains("xcopy", await File.ReadAllTextAsync(scriptPath));
        Assert.True(service.IsUpdatePending);
    }

    // ──────────────────────────────────
    // CleanupStaleUpdates
    // ──────────────────────────────────

    [Fact]
    public void Cleanup_RemovesTmpAndZipFiles()
    {
        var updatesDir = Path.Combine(_testDir, "updates");
        File.WriteAllText(Path.Combine(updatesDir, "stale.zip"), "zip");
        File.WriteAllText(Path.Combine(updatesDir, "stale.tmp"), "tmp");
        File.WriteAllText(Path.Combine(updatesDir, "stale2.ZIP"), "ZIP");

        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);

        // Constructor already called CleanupStaleUpdates, but because the
        // test directory was set up AFTER base constructor, call it explicitly.
        service.CleanupStaleUpdates();

        Assert.False(File.Exists(Path.Combine(updatesDir, "stale.zip")));
        Assert.False(File.Exists(Path.Combine(updatesDir, "stale.tmp")));
        Assert.False(File.Exists(Path.Combine(updatesDir, "stale2.ZIP")));
    }

    [Fact]
    public void Cleanup_NoDirectory_DoesNotThrow()
    {
        var updatesDir = Path.Combine(_testDir, "updates");
        Directory.Delete(updatesDir, recursive: true);

        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var ex = Record.Exception(() =>
        {
            var service = CreateService(handler.Object);
            service.CleanupStaleUpdates();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void Cleanup_RemovesStaleBatchFile()
    {
        var updatesDir = Path.Combine(_testDir, "updates");
        File.WriteAllText(Path.Combine(updatesDir, "update.bat"), "old script");

        var handler = CreateHandler(HttpStatusCode.OK, "{}");
        var service = CreateService(handler.Object);
        service.CleanupStaleUpdates();

        Assert.False(File.Exists(Path.Combine(updatesDir, "update.bat")));
    }

    // ──────────────────────────────────
    // GithubRelease model
    // ──────────────────────────────────

    [Fact]
    public void GithubRelease_Version_WithVPrefix()
    {
        var release = MakeRelease("v3.2.1");
        Assert.Equal(new Version(3, 2, 1), release.Version);
    }

    [Fact]
    public void GithubRelease_Version_WithoutVPrefix()
    {
        var release = MakeRelease("3.2.1");
        Assert.Equal(new Version(3, 2, 1), release.Version);
    }

    [Fact]
    public void GithubRelease_Version_InvalidTag_ReturnsNull()
    {
        var release = MakeRelease("invalid");
        Assert.Null(release.Version);
    }

    [Fact]
    public void GithubRelease_Version_EmptyTag_ReturnsNull()
    {
        var release = MakeRelease("");
        Assert.Null(release.Version);
    }

    // ──────────────────────────────────
    // Version comparison integration
    // ──────────────────────────────────

    [Fact]
    public async Task MinorVersionBump_IsDetected()
    {
        var json = ReleaseJson("v1.1.0");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task PatchVersionBump_IsDetected()
    {
        var json = ReleaseJson("v1.0.1");
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task Prerelease_StillOffered_IfVersionHigher()
    {
        var json = @"{
            ""tag_name"": ""v2.0.0-beta1"",
            ""html_url"": ""..."",
            ""prerelease"": true,
            ""assets"": []
        }";
        var handler = CreateHandler(HttpStatusCode.OK, json);
        var service = CreateService(handler.Object);

        var result = await service.CheckForUpdateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.Prerelease);
    }

    // ──────────────────────────────────
    // Deserialization
    // ──────────────────────────────────

    [Fact]
    public void Deserialize_FullRelease_ParsesCorrectly()
    {
        var json = @"{
            ""tag_name"": ""v1.2.3"",
            ""html_url"": ""https://github.com/repo/releases/tag/v1.2.3"",
            ""body"": ""Release notes here"",
            ""prerelease"": false,
            ""assets"": [
                { ""name"": ""app-v1.2.3.zip"", ""browser_download_url"": ""https://example.com/dl.zip"", ""size"": 12345 }
            ]
        }";

        var release = JsonSerializer.Deserialize<GithubRelease>(json);

        Assert.NotNull(release);
        Assert.Equal("v1.2.3", release!.TagName);
        Assert.Equal(new Version(1, 2, 3), release.Version);
        Assert.Single(release.Assets);
        Assert.Equal("app-v1.2.3.zip", release.Assets[0].Name);
        Assert.Equal(12345, release.Assets[0].Size);
    }
}
