using System.Windows.Input;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAppSettingsService _settings;
    private string _apiKey = "";
    private string _alertThreshold = "2.00";
    private int _pollingIntervalMin = 15;
    private string _selectedLang = "cs";
    private string _logPath = "";
    private string _dbPath = "";
    private string _status = "";
    private string _appVersion = "";
    private List<LangOption> _availableLangs = new();

    public SettingsViewModel(IAppSettingsService settings, IUpdateService updateService)
    {
        _settings = settings;
        AppVersion = $"v{updateService.CurrentVersion}";
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        TestNotificationCommand = new RelayCommand(async _ => await TestNotificationAsync());
    }

    public ICommand TestNotificationCommand { get; }

    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string AlertThreshold { get => _alertThreshold; set => SetProperty(ref _alertThreshold, value); }
    public int PollingIntervalMin { get => _pollingIntervalMin; set => SetProperty(ref _pollingIntervalMin, value); }
    public string SelectedLang { get => _selectedLang; set => SetProperty(ref _selectedLang, value); }
    public string LogPath { get => _logPath; set => SetProperty(ref _logPath, value); }
    public string DbPath { get => _dbPath; set => SetProperty(ref _dbPath, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }
    public string AppVersion { get => _appVersion; set => SetProperty(ref _appVersion, value); }
    public List<LangOption> AvailableLangs { get => _availableLangs; set => SetProperty(ref _availableLangs, value); }

    public ICommand SaveCommand { get; }
    public List<int> IntervalOptions { get; } = new() { 5, 10, 15, 30, 60 };

    public async Task LoadAsync()
    {
        var loc = LocalizationService.Instance;

        AvailableLangs = loc.GetAvailableLangs()
            .Select(code => new LangOption
            {
                Code = code,
                DisplayName = loc.GetLangDisplayName(code)
            })
            .ToList();

        var key = await _settings.GetApiKeyAsync();
        ApiKey = key ?? "";
        AlertThreshold = (await _settings.GetAlertThresholdAsync()) ?? "2.00";
        PollingIntervalMin = await _settings.GetPollingIntervalMinutesAsync();
        SelectedLang = (await _settings.GetLanguageAsync()) ?? "cs";
        LogPath = (await _settings.GetLogPathAsync()) ?? "";
        DbPath = (await _settings.GetDbPathAsync()) ?? "";
        Status = loc["status_loaded"];
    }

    public async Task SaveAsync()
    {
        var loc = LocalizationService.Instance;

        if (!string.IsNullOrWhiteSpace(ApiKey))
            await _settings.SetApiKeyAsync(ApiKey.Trim());
        await _settings.SetAlertThresholdAsync(AlertThreshold);
        await _settings.SetPollingIntervalMinutesAsync(PollingIntervalMin);
        await _settings.SetLanguageAsync(SelectedLang);
        await _settings.SetLogPathAsync(LogPath);
        await _settings.SetDbPathAsync(DbPath);

        Status = loc["status_saved"];

        System.Diagnostics.Process.Start(
            System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
        System.Windows.Application.Current.Shutdown();
    }

    public async Task TestNotificationAsync()
    {
        var loc = LocalizationService.Instance;
        var threshold = decimal.TryParse(AlertThreshold, out var t) ? t : 2.00m;
        var testBalance = threshold / 2;
        var msg = loc.Format("notification_low_balance",
            $"${threshold:F2}", $"${testBalance:F2}");

        Windows.NotificationToast.Show(msg);
        Status = $"🔔 Test hotov (práh: ${threshold:F2})";
    }
}

public class LangOption
{
    public string Code { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? param) => _canExecute?.Invoke(param) ?? true;
    public void Execute(object? param) => _execute(param);
}
