using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
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
    private CancellationTokenSource _pollCts = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Aplikace se ukončí jen explicitně — ne při zavření všech oken
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekCreditCheck");
        Directory.CreateDirectory(appDir);

        var dbPath = Path.Combine(appDir, "data.db");
        var logPath = Path.Combine(appDir, "app.log");

        // Logging
        Logger.Init(logPath);

        var services = new ServiceCollection();

        // DB
        var db = new AppDbContext(dbPath);
        db.InitializeAsync().GetAwaiter().GetResult();
        services.AddSingleton(db);

        // Repositories
        services.AddSingleton<IBalanceRepository, BalanceRepository>();

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

        // Eventy
        var polling = _services.GetRequiredService<IPollingService>();
        polling.PollCompleted += (_, result) =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                _trayIcon.UpdateTooltip(result);
                _trayIcon.SetError("");
                var dashboardVm = _services.GetRequiredService<DashboardViewModel>();
                dashboardVm.OnPollCompleted(result);
            });
        };

        polling.PollFailed += (_, message) =>
        {
            Dispatcher.BeginInvoke(() => _trayIcon.SetError(message));
        };

        var alertService = _services.GetRequiredService<AlertService>();
        alertService.AlertTriggered += (_, args) =>
        {
            Dispatcher.BeginInvoke(() => _trayIcon.ShowNotification(args.Message));
        };

        // Spustit polling s mírným zpožděním — až po inicializaci UI
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(async () =>
        {
            await polling.StartAsync(_pollCts.Token);
        }));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pollCts.Cancel();
        _pollCts.Dispose();
        _trayIcon.Dispose();
        base.OnExit(e);
    }
}
