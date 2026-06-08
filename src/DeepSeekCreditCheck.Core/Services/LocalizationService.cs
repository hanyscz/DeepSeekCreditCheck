using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DeepSeekCreditCheck.Core.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private Dictionary<string, string> _strings = new();
    private string _currentLang = "cs";
    private string _langDir;

    public static LocalizationService Instance { get; } = new();

    public string CurrentLang => _currentLang;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] =>
        _strings.TryGetValue(key, out var v) ? v : $"[{key}]";

    public string Format(string key, params object?[] args)
    {
        var template = this[key];
        return args.Length > 0 ? string.Format(template, args) : template;
    }

    public void SetLangDir(string dir)
    {
        _langDir = dir;
    }

    public List<string> GetAvailableLangs()
    {
        if (string.IsNullOrEmpty(_langDir) || !Directory.Exists(_langDir))
            return new List<string> { "cs" };

        return Directory.GetFiles(_langDir, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => x != null)
            .Cast<string>()
            .OrderBy(x => x)
            .ToList();
    }

    public string GetLangDisplayName(string langCode)
    {
        var path = Path.Combine(_langDir, $"{langCode}.json");
        if (!File.Exists(path)) return langCode;

        try
        {
            var doc = JsonDocument.Parse(File.ReadAllText(path));
            return doc.RootElement.TryGetProperty("lang_name", out var name)
                ? name.GetString() ?? langCode
                : langCode;
        }
        catch
        {
            return langCode;
        }
    }

    public void SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(_langDir)) return;

        var path = Path.Combine(_langDir, $"{langCode}.json");
        if (!File.Exists(path)) return;

        try
        {
            var json = File.ReadAllText(path);
            _strings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                       ?? new Dictionary<string, string>();
            _currentLang = langCode;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CurrentLang"));
        }
        catch
        {
            // neplatny JSON — ignorovat
        }
    }
}
