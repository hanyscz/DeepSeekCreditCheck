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

        var defaultDbPath = Path.Combine(appDir, "data.db");
        var defaultLogPath = Path.Combine(appDir, "app.log");

        // Bootstrap — načíst nastavení z výchozí DB (DbPath, LogPath, Language)
        var bootstrapDb = new AppDbContext(defaultDbPath);
        bootstrapDb.InitializeAsync().GetAwaiter().GetResult();
        var bootstrapSettings = new AppSettingsService(bootstrapDb);
        var customDbPath = bootstrapSettings.GetDbPathAsync().GetAwaiter().GetResult();
        var dbPath = !string.IsNullOrWhiteSpace(customDbPath) ? customDbPath : defaultDbPath;
        if (dbPath != defaultDbPath)
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var customLogPath = bootstrapSettings.GetLogPathAsync().GetAwaiter().GetResult();
        var logPath = !string.IsNullOrWhiteSpace(customLogPath) ? customLogPath : defaultLogPath;
        var appDirForLog = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(appDirForLog)) Directory.CreateDirectory(appDirForLog);

        var savedLang = bootstrapSettings.GetLanguageAsync().GetAwaiter().GetResult() ?? "cs";

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
        services.AddSingleton<IUpdateService, UpdateService>();

        // ViewModels
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<SettingsViewModel>();

        _services = services.BuildServiceProvider();

        // Načíst jazykové soubory
        var langDir = FindLangDir();
        var loc = LocalizationService.Instance;
        loc.SetLangDir(langDir);
        loc.SetLanguage(savedLang);

        // Tray icon
        _trayIcon = new TrayIconService(_services);
        _trayIcon.Initialize();

        // Přečíst marker hned (než ho CleanupStaleUpdates smaže), ale notifikaci ukázat až po načtení UI
        var updateSuccessVersion = _services.GetRequiredService<IUpdateService>().ConsumeSuccessMarker();

        // Eventy
        var polling = _services.GetRequiredService<IPollingService>();
        polling.PollCompleted += (_, result) =>
        {
            Dispatcher.BeginInvoke(async () =>
            {
                var dashboardVm = _services.GetRequiredService<DashboardViewModel>();
                await dashboardVm.OnPollCompleted(result);
                _trayIcon.UpdateTooltip(result);
                _trayIcon.SetError("");
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

            // Notifikace o úspěšné aktualizaci — s dostatečným odstupem, spolehlivé i po startu z batch skriptu
            if (updateSuccessVersion != null)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                _trayIcon.ShowNotification(loc.Format("update_success_notify", updateSuccessVersion));
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
            await _trayIcon.CheckForUpdatesOnStartupAsync();
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
