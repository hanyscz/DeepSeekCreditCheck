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

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Logger.Error("CRITICAL: AppDomain unhandled exception", ex);
        };
        DispatcherUnhandledException += (_, args) =>
        {
            Logger.Error("CRITICAL: Dispatcher unhandled exception", args.Exception);
            args.Handled = true;
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Logger.Error("CRITICAL: Task unobserved exception", args.Exception);
            args.SetObserved();
        };

        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DeepSeekCreditCheck");
        Directory.CreateDirectory(appDir);

        var dbPath = Path.Combine(appDir, "data.db");
        var defaultLogPath = Path.Combine(appDir, "app.log");

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

        // Najít adresář s Lang/ (vedle exe nebo o 1 uroven vyse)
        var langDir = FindLangDir();
        var loc = LocalizationService.Instance;
        loc.SetLangDir(langDir);

        // Načíst jazyk z nastavení
        var settings = _services.GetRequiredService<IAppSettingsService>();
        var savedLang = settings.GetLanguageAsync().GetAwaiter().GetResult() ?? "cs";
        loc.SetLanguage(savedLang);

        // Načíst log path z nastavení (nebo výchozí)
        var customLogPath = settings.GetLogPathAsync().GetAwaiter().GetResult();
        var logPath = !string.IsNullOrWhiteSpace(customLogPath) ? customLogPath : defaultLogPath;
        Logger.Init(logPath);
        var appDirForLog = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(appDirForLog))
            Directory.CreateDirectory(appDirForLog);

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

    private static string FindLangDir()
    {
        var candidates = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Lang"),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
                         "DeepSeekCreditCheck.Core", "Lang"),
        };
        foreach (var dir in candidates)
        {
            var resolved = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dir));
            if (Directory.Exists(resolved)) return resolved;
        }
        return candidates[0];
    }
}
