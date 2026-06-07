using System.IO;
using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using DeepSeekCreditCheck.Core.Data;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using DeepSeekCreditCheck.UI.Services;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI;

public partial class App : Application
{
    private IServiceProvider _services = null!;
    private TrayIconService _trayIcon = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekCreditCheck", "data.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        // DB
        var db = new AppDbContext(dbPath);
        await db.InitializeAsync();
        services.AddSingleton(db);

        // Repositories
        services.AddSingleton<IBalanceRepository, BalanceRepository>();
        services.AddSingleton<IUsageRepository, UsageRepository>();

        // Services
        services.AddSingleton<IAppSettingsService, AppSettingsService>();
        services.AddSingleton<HttpClient>();
        services.AddSingleton<IDeepSeekApiClient, DeepSeekApiClient>();
        services.AddSingleton<PredictionEngine>();
        services.AddSingleton<AlertService>();
        services.AddSingleton<IPollingService, PollingService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();

        _services = services.BuildServiceProvider();

        // Tray icon
        _trayIcon = new TrayIconService(_services);
        _trayIcon.Initialize();

        // Spustit polling
        var polling = _services.GetRequiredService<IPollingService>();
        var cts = new CancellationTokenSource();
        await polling.StartAsync(cts.Token);

        // Eventy
        polling.PollCompleted += (_, result) =>
        {
            Dispatcher.Invoke(() =>
            {
                _trayIcon.UpdateTooltip(result);
                var dashboardVm = _services.GetRequiredService<DashboardViewModel>();
                dashboardVm.OnPollCompleted(result);
            });
        };

        var alertService = _services.GetRequiredService<AlertService>();
        alertService.AlertTriggered += (_, args) =>
        {
            Dispatcher.Invoke(() => _trayIcon.ShowNotification(args.Message));
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon.Dispose();
        base.OnExit(e);
    }
}
