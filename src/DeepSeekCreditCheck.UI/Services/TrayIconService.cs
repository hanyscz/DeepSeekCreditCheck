using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
    private TextBlock? _tooltipText;
    private DashboardWindow? _dashboardWindow;

    public TrayIconService(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize()
    {
        var loc = LocalizationService.Instance;

        _notifyIcon = new TaskbarIcon
        {
            Icon = CreateAppIcon(),
            Visibility = Visibility.Visible
        };

        // Custom tooltip ovládací prvek — 2x větší, černý podklad
        _tooltipText = new TextBlock
        {
            Text = "DeepSeek Credit Check",
            FontSize = 20,
            Foreground = new SolidColorBrush(Colors.White),
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(14)
        };

        var tooltipBorder = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(40, 40, 40)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Child = _tooltipText,
            Padding = new Thickness(4)
        };

        _notifyIcon.TrayToolTip = tooltipBorder;

        var menu = new System.Windows.Controls.ContextMenu();

        var balanceItem = new System.Windows.Controls.MenuItem
        {
            Header = "💰 " + loc["loading"],
            IsEnabled = false
        };
        menu.Items.Add(balanceItem);

        var predictionItem = new System.Windows.Controls.MenuItem
        {
            Header = "📊 " + loc["loading"],
            IsEnabled = false
        };
        menu.Items.Add(predictionItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var errorItem = new System.Windows.Controls.MenuItem
        {
            Header = "",
            IsEnabled = false,
            Visibility = Visibility.Collapsed
        };
        menu.Items.Add(errorItem);

        var dashboardItem = new System.Windows.Controls.MenuItem { Header = loc["tray_dashboard"] };
        dashboardItem.Click += (_, _) => OpenDashboard();
        menu.Items.Add(dashboardItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = loc["tray_settings"] };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var refreshItem = new System.Windows.Controls.MenuItem { Header = loc["tray_refresh"] };
        refreshItem.Click += async (_, _) =>
        {
            var polling = _services.GetRequiredService<IPollingService>();
            if (polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        };
        menu.Items.Add(refreshItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = loc["tray_exit"] };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenu = menu;
        _notifyIcon.TrayMouseDoubleClick += (_, _) => OpenDashboard();

        _notifyIcon.Tag = new TrayMenuRefs
        {
            BalanceItem = balanceItem,
            PredictionItem = predictionItem,
            ErrorItem = errorItem
        };
    }

    public void UpdateTooltip(PollResult result)
    {
        if (_notifyIcon?.Tag is not TrayMenuRefs refs) return;

        var loc = LocalizationService.Instance;
        var bal = result.Snapshot?.TotalBalanceDecimal ?? 0;
        var balStr = $"${bal:F2}";
        var pred = result.Prediction?.FormattedPrediction ?? "—";

        refs.BalanceItem.Header = loc.Format("tray_balance", balStr);
        refs.PredictionItem.Header = loc.Format("tray_prediction", pred);

        refs.ErrorItem.Visibility = Visibility.Collapsed;

        // Custom tooltip — 2x větší
        if (_tooltipText != null)
        {
            _tooltipText.Text = $"{loc["tooltip_title"]}\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"{loc.Format("tooltip_balance", balStr)}\n" +
                $"{loc.Format("tooltip_prediction", pred)}\n" +
                $"🕐 {DateTime.Now:HH:mm:ss}";
        }
    }

    public void SetError(string message)
    {
        if (_notifyIcon?.Tag is not TrayMenuRefs refs) return;

        if (string.IsNullOrEmpty(message))
        {
            refs.ErrorItem.Visibility = Visibility.Collapsed;
            refs.ErrorItem.Header = "";
        }
        else
        {
            refs.ErrorItem.Visibility = Visibility.Visible;
            refs.ErrorItem.Header = $"⚠️ {message}";
        }
    }

    public void ShowNotification(string message)
    {
        _notifyIcon?.ShowBalloonTip("DeepSeek Credit Check", message, BalloonIcon.Warning);
    }

    private void OpenDashboard()
    {
        if (_dashboardWindow == null || !_dashboardWindow.IsLoaded)
        {
            _dashboardWindow = new DashboardWindow(_services.GetRequiredService<DashboardViewModel>());
            _dashboardWindow.Show();
        }
        else
        {
            _dashboardWindow.Show();
            _dashboardWindow.Activate();
        }
    }

    private void OpenSettings()
    {
        var window = new SettingsWindow(_services.GetRequiredService<SettingsViewModel>());
        window.ShowDialog();
    }

    private static System.Drawing.Icon CreateAppIcon()
    {
        try
        {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var customPath = Path.Combine(exeDir, "Resources", "app.ico");
            if (File.Exists(customPath))
                return new System.Drawing.Icon(customPath);
        }
        catch { }

        using var bitmap = new System.Drawing.Bitmap(16, 16);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(System.Drawing.Color.FromArgb(0, 120, 215));
        using var font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold);
        using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
        g.DrawString("$", font, brush, 3, -1);
        using var temp = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        return (System.Drawing.Icon)temp.Clone();
    }

    public void Dispose()
    {
        _notifyIcon?.Dispose();
    }

    private class TrayMenuRefs
    {
        public System.Windows.Controls.MenuItem BalanceItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem PredictionItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem ErrorItem { get; set; } = null!;
    }
}
