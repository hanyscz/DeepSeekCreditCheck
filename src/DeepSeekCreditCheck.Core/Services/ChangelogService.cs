using System.Reflection;
using System.Text.RegularExpressions;

namespace DeepSeekCreditCheck.Core.Services;

public partial class ChangelogService : IChangelogService
{
    private static readonly Lazy<ChangelogService> _instance = new(() => new ChangelogService());
    public static ChangelogService Instance => _instance.Value;

    [GeneratedRegex(@"^##\s+(v?[\d\.]+)\s*(?:\(([^)]+)\))?", RegexOptions.Multiline)]
    private static partial Regex VersionHeaderRegex();

    public string GetChangelogText(string? lang = null)
    {
        var language = lang ?? LocalizationService.Instance.CurrentLang;
        var isCzech = string.Equals(language, "cs", StringComparison.OrdinalIgnoreCase);
        var primaryFilename = isCzech ? "CHANGELOG_CZ.md" : "CHANGELOG.md";
        var fallbackFilename = isCzech ? "CHANGELOG.md" : "CHANGELOG_CZ.md";

        var content = TryLoadFileFromDisk(primaryFilename) ?? TryLoadEmbeddedResource(primaryFilename);
        if (!string.IsNullOrWhiteSpace(content))
        {
            return content;
        }

        // Fallback na druhý soubor
        content = TryLoadFileFromDisk(fallbackFilename) ?? TryLoadEmbeddedResource(fallbackFilename);
        return content ?? "# Changelog\n\nŽádné záznamy nebyly nalezeny.";
    }

    public IReadOnlyList<ChangelogVersion> GetVersions(string? lang = null)
    {
        var text = GetChangelogText(lang);
        var list = new List<ChangelogVersion>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return list;
        }

        var regex = VersionHeaderRegex();
        var matches = regex.Matches(text);

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            var version = match.Groups[1].Value.Trim();
            var date = match.Groups[2].Success ? match.Groups[2].Value.Trim() : string.Empty;
            var header = match.Value.Trim();

            int startIndex = match.Index + match.Length;
            int endIndex = (i + 1 < matches.Count) ? matches[i + 1].Index : text.Length;

            var rawBody = text.Substring(startIndex, endIndex - startIndex).Trim();
            // Odstranit případné koncové oddělovače ---
            if (rawBody.EndsWith("---"))
            {
                rawBody = rawBody.Substring(0, rawBody.Length - 3).Trim();
            }

            list.Add(new ChangelogVersion
            {
                Version = version,
                Date = date,
                Header = header,
                Content = rawBody
            });
        }

        return list;
    }

    private static string? TryLoadFileFromDisk(string filename)
    {
        try
        {
            // 1. Zkontrolujeme BaseDirectory
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(baseDir, filename);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            // 2. Zkontrolujeme CurrentDirectory
            var currentDir = Directory.GetCurrentDirectory();
            path = Path.Combine(currentDir, filename);
            if (File.Exists(path))
            {
                return File.ReadAllText(path);
            }

            // 3. Prohledáme nadřazené složky (běžné při spuštění v IDE)
            var dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 5 && dir != null; i++)
            {
                path = Path.Combine(dir.FullName, filename);
                if (File.Exists(path))
                {
                    return File.ReadAllText(path);
                }
                dir = dir.Parent;
            }
        }
        catch
        {
            // Tichý fallback na embedded resources
        }

        return null;
    }

    private static string? TryLoadEmbeddedResource(string filename)
    {
        try
        {
            var assembly = typeof(ChangelogService).Assembly;
            var resourceNames = assembly.GetManifestResourceNames();
            var matchedName = resourceNames.FirstOrDefault(n => n.EndsWith(filename, StringComparison.OrdinalIgnoreCase));

            if (matchedName != null)
            {
                using var stream = assembly.GetManifestResourceStream(matchedName);
                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
            }
        }
        catch
        {
            // Ignorovat chyby
        }

        return null;
    }
}
