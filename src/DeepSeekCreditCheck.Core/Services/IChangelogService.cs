namespace DeepSeekCreditCheck.Core.Services;

public class ChangelogVersion
{
    public string Version { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Header { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public interface IChangelogService
{
    string GetChangelogText(string? lang = null);
    IReadOnlyList<ChangelogVersion> GetVersions(string? lang = null);
}
