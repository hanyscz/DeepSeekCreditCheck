using DeepSeekCreditCheck.Core.Models;

namespace DeepSeekCreditCheck.Core.Services;

public interface IUpdateService
{
    Version CurrentVersion { get; }

    bool IsUpdatePending { get; }

    GithubRelease? PendingRelease { get; }

    event Action<GithubRelease>? UpdateAvailable;

    Task<GithubRelease?> CheckForUpdateAsync(CancellationToken ct);

    Task DownloadAndExtractAsync(GithubRelease release, IProgress<double>? progress, CancellationToken ct);

    Task<string> PrepareUpdateScriptAsync(GithubRelease release);

    Task<string> DownloadAndApplyAsync(IProgress<double>? progress, CancellationToken ct);

    void MarkUpdateAvailable(GithubRelease release);

    /// <summary>Zapíše marker soubor, že aktualizace na danou verzi proběhla úspěšně.</summary>
    void WriteSuccessMarker(string version);

    /// <summary>Zkontroluje, zda existuje marker soubor z předchozí úspěšné aktualizace. Pokud ano, smaže ho a vrátí verzi.</summary>
    string? ConsumeSuccessMarker();

    void CleanupStaleUpdates();
}
