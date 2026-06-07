using System.Windows.Input;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly IAppSettingsService _settings;
    private string _apiKey = "";
    private string _alertThreshold = "2.00";
    private int _pollingIntervalMin = 15;
    private string _status = "";

    public SettingsViewModel(IAppSettingsService settings)
    {
        _settings = settings;
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        LoadCommand = new RelayCommand(async _ => await LoadAsync());
    }

    public string ApiKey { get => _apiKey; set => SetProperty(ref _apiKey, value); }
    public string AlertThreshold { get => _alertThreshold; set => SetProperty(ref _alertThreshold, value); }
    public int PollingIntervalMin { get => _pollingIntervalMin; set => SetProperty(ref _pollingIntervalMin, value); }
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    public ICommand SaveCommand { get; }
    public ICommand LoadCommand { get; }

    public List<int> IntervalOptions { get; } = new() { 5, 10, 15, 30, 60 };

    public async Task LoadAsync()
    {
        var key = await _settings.GetApiKeyAsync();
        ApiKey = key ?? "";
        AlertThreshold = (await _settings.GetAlertThresholdAsync()) ?? "2.00";
        PollingIntervalMin = await _settings.GetPollingIntervalMinutesAsync();
        Status = "Načteno";
    }

    public async Task SaveAsync()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            await _settings.SetApiKeyAsync(ApiKey.Trim());
        await _settings.SetAlertThresholdAsync(AlertThreshold);
        await _settings.SetPollingIntervalMinutesAsync(PollingIntervalMin);
        Status = "✅ Uloženo";

        // Trigger re-poll with new settings
        System.Diagnostics.Process.Start(
            System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
        System.Windows.Application.Current.Shutdown();
    }
}

// Jednoduchý RelayCommand
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
