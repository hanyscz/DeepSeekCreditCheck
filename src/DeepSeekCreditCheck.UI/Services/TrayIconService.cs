using System.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using DeepSeekCreditCheck.Core.Services;
using DeepSeekCreditCheck.UI.ViewModels;
using DeepSeekCreditCheck.UI.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace DeepSeekCreditCheck.UI.Services;

public class TrayIconService : IDisposable
{
    private readonly IServiceProvider _services;
    private TaskbarIcon? _notifyIcon;

    public TrayIconService(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize()
    {
        _notifyIcon = new TaskbarIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(
                System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("No main module")),
            ToolTipText = "DeepSeek Credit Check",
            Visibility = Visibility.Visible
        };

        var menu = new System.Windows.Controls.ContextMenu();

        // Info items
        var balanceItem = new System.Windows.Controls.MenuItem
        {
            Header = "\U0001f4b0 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(balanceItem);

        var toppedUpItem = new System.Windows.Controls.MenuItem
        {
            Header = "\U0001f501 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(toppedUpItem);

        var predictionItem = new System.Windows.Controls.MenuItem
        {
            Header = "\U0001f4ca Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(predictionItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        // Actions
        var dashboardItem = new System.Windows.Controls.MenuItem { Header = "\U0001f4c8 Dashboard" };
        dashboardItem.Click += (_, _) => OpenDashboard();
        menu.Items.Add(dashboardItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙️ Nastavení" };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "\U0001f504 Obnovit teď" };
        refreshItem.Click += async (_, _) =>
        {
            var polling = _services.GetRequiredService<IPollingService>();
            if (polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        };
        menu.Items.Add(refreshItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = "❌ Ukončit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenu = menu;
        _notifyIcon.TrayMouseDoubleClick += (_, _) => OpenDashboard();

        _notifyIcon.Tag = new TrayMenuRefs
        {
            BalanceItem = balanceItem,
            ToppedUpItem = toppedUpItem,
            PredictionItem = predictionItem
        };
    }

    public void UpdateTooltip(PollResult result)
    {
        if (_notifyIcon?.Tag is not TrayMenuRefs refs) return;

        var bal = result.Snapshot?.TotalBalanceDecimal ?? 0;
        var topped = result.Snapshot?.ToppedUpBalanceDecimal ?? 0;
        var pred = result.Prediction?.FormattedPrediction ?? "—";

        refs.BalanceItem.Header = $"\U0001f4b0 ${bal:F2} zbývá";
        refs.ToppedUpItem.Header = topped > 0
            ? $"\U0001f501 Z toho ${topped:F2} vlastní"
            : "\U0001f501 Všechno vlastní kredit";
        refs.PredictionItem.Header = $"\U0001f4ca {pred}";

        _notifyIcon.ToolTipText = $"Zůstatek: ${bal:F2} | Predikce: {pred} | {DateTime.Now:HH:mm}";
    }

    public void ShowNotification(string message)
    {
        _notifyIcon?.ShowBalloonTip("DeepSeek Credit Check", message, BalloonIcon.Warning);
    }

    private void OpenDashboard()
    {
        var window = new DashboardWindow(_services.GetRequiredService<DashboardViewModel>());
        window.Show();
        window.Activate();
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_services.GetRequiredService<SettingsViewModel>());
        window.ShowDialog();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }

    private class TrayMenuRefs
    {
        public System.Windows.Controls.MenuItem BalanceItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem ToppedUpItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem PredictionItem { get; set; } = null!;
    }
}
