using System.Text.Json.Serialization;

namespace DeepSeekCreditCheck.Core.Models;

public class GithubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string? Body { get; set; }

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("assets")]
    public List<GithubReleaseAsset> Assets { get; set; } = [];

    public Version? Version
    {
        get
        {
            var clean = TagName.TrimStart('v').Split('+', '-', ' ')[0];
            return Version.TryParse(clean, out var v) ? v : null;
        }
    }
}

public class GithubReleaseAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string DownloadUrl { get; set; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
