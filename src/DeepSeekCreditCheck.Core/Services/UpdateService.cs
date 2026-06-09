using System.IO.Compression;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public class UpdateService : IUpdateService
{
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    private const string Owner = "hanyscz";
    private const string Repo = "DeepSeekCreditCheck";
    private const string UpdateDirName = "updates";

    public Version CurrentVersion { get; }
    public bool IsUpdatePending { get; private set; }
    public GithubRelease? PendingRelease { get; private set; }

    public event Action<GithubRelease>? UpdateAvailable;

    public UpdateService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        CurrentVersion = ReadCurrentVersion();
        CleanupStaleUpdates();
    }

    public async Task<GithubRelease?> CheckForUpdateAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest");

        request.Headers.UserAgent.ParseAdd($"DeepSeekCreditCheck/{CurrentVersion}");
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(ct);
        var release = JsonSerializer.Deserialize<GithubRelease>(json);

        if (release?.Version == null)
            return null;

        return release.Version > CurrentVersion ? release : null;
    }

    public async Task DownloadAndExtractAsync(
        GithubRelease release,
        IProgress<double>? progress,
        CancellationToken ct)
    {
        var updatesDir = GetUpdatesDir();
        Directory.CreateDirectory(updatesDir);

        var zipPath = Path.Combine(updatesDir, $"{release.TagName}.zip");
        var extractDir = Path.Combine(updatesDir, release.TagName);

        var asset = release.Assets.FirstOrDefault(a =>
            a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                $"No zip asset found in release {release.TagName}");

        using var response = await _httpClient.GetAsync(
            asset.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead,
            ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        // Stáhnout do dočasného souboru, aby se ZIP na disku neotevíral přes File.Create
        var tempPath = zipPath + ".tmp";
        {
            await using var fileStream = File.Create(tempPath);
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                totalRead += bytesRead;
                if (totalBytes > 0)
                    progress?.Report((double)totalRead / totalBytes);
            }
        }
        // Soubor je zde už zavřený (konec using bloku)
        File.Move(tempPath, zipPath, overwrite: true);

        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, recursive: true);

        ZipFile.ExtractToDirectory(zipPath, extractDir);

        File.Delete(zipPath);

        IsUpdatePending = true;
        PendingRelease = release;
    }

    public Task<string> PrepareUpdateScriptAsync(GithubRelease release)
    {
        var appDir = AppDomain.CurrentDomain.BaseDirectory;
        var updatesDir = GetUpdatesDir();
        var updateDir = Path.Combine(updatesDir, release.TagName);
        var scriptPath = Path.Combine(updatesDir, "update.bat");

        // Zapsat marker teď (ve staré appce) — spolehlivější než z batch skriptu
        WriteSuccessMarker(release.TagName);

        var script = GenerateBatchScript(appDir, updateDir);
        File.WriteAllText(scriptPath, script);

        return Task.FromResult(scriptPath);
    }

    public async Task<string> DownloadAndApplyAsync(IProgress<double>? progress, CancellationToken ct)
    {
        if (PendingRelease == null)
            throw new InvalidOperationException("No pending update. Call CheckForUpdateAsync first.");

        await _updateLock.WaitAsync(ct);
        try
        {
            var release = PendingRelease;
            await DownloadAndExtractAsync(release, progress, ct);
            return await PrepareUpdateScriptAsync(release);
        }
        finally
        {
            _updateLock.Release();
        }
    }

    public void MarkUpdateAvailable(GithubRelease release)
    {
        PendingRelease = release;
        IsUpdatePending = true;
        UpdateAvailable?.Invoke(release);
    }

    public void WriteSuccessMarker(string version)
    {
        var path = Path.Combine(GetUpdatesDir(), "update_success.txt");
        File.WriteAllText(path, version);
    }

    public string? ConsumeSuccessMarker()
    {
        try
        {
            var path = Path.Combine(GetUpdatesDir(), "update_success.txt");
            if (!File.Exists(path)) return null;
            var version = File.ReadAllText(path).Trim();
            TryDeleteFile(path);
            return version;
        }
        catch
        {
            return null;
        }
    }

    public void CleanupStaleUpdates()
    {
        try
        {
            var updatesDir = GetUpdatesDir();
            if (!Directory.Exists(updatesDir)) return;

            // Vyčistit staré .tmp a .zip soubory z přerušených downloadů
            foreach (var f in Directory.GetFiles(updatesDir, "*.tmp"))
                TryDeleteFile(f);
            foreach (var f in Directory.GetFiles(updatesDir, "*.zip"))
                TryDeleteFile(f);

            // Vyčistit adresáře extrahovaných verzí starších než 7 dní
            foreach (var dir in Directory.GetDirectories(updatesDir))
            {
                try
                {
                    var dirInfo = new DirectoryInfo(dir);
                    if (dirInfo.Name != "update.bat" && (DateTime.UtcNow - dirInfo.LastWriteTimeUtc).TotalDays > 7)
                        Directory.Delete(dir, recursive: true);
                }
                catch { }
            }

            // Vyčistit staré update.bat soubory
            var batchPath = Path.Combine(updatesDir, "update.bat");
            if (File.Exists(batchPath))
                TryDeleteFile(batchPath);
        }
        catch { }
    }

    protected virtual Version ReadCurrentVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        var attr = assembly?.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        if (attr != null)
        {
            // InformationalVersion může obsahovat metadata (např. "1.1.0+commithash")
            var raw = attr.InformationalVersion;
            var clean = raw.Split('+', '-', ' ')[0];
            if (Version.TryParse(clean, out var v))
                return v;
        }

        return new Version(1, 0, 0);
    }

    protected virtual string GetUpdatesDir()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "DeepSeekCreditCheck", UpdateDirName);
    }

    private static string GenerateBatchScript(string appDir, string updateDir)
    {
        if (!appDir.EndsWith('\\')) appDir += '\\';

        return $"""
            @echo off
            chcp 65001 >nul
            title DeepSeek Credit Check — Updating

            :wait
            timeout /T 1 /NOBREAK >NUL
            tasklist /FI "IMAGENAME eq DeepSeekCreditCheck.UI.exe" 2>NUL | find /I /N "DeepSeekCreditCheck.UI.exe" >NUL
            if "%ERRORLEVEL%"=="0" goto wait

            xcopy "{updateDir}\*" "{appDir}" /E /Y /Q >nul
            start "" "{appDir}DeepSeekCreditCheck.UI.exe"
            exit
            """;
    }

    private static void TryDeleteFile(string path)
    {
        try { File.Delete(path); } catch { }
    }
}
