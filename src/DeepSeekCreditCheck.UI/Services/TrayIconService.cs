using System.IO;
using System.Globalization;
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
    private decimal _lastBalance;

    public TrayIconService(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize()
    {
        _notifyIcon = new TaskbarIcon
        {
            Icon = CreateBalanceIcon(0),
            ToolTipText = "DeepSeek Credit Check",
            Visibility = Visibility.Visible
        };

        var menu = new System.Windows.Controls.ContextMenu();

        var balanceItem = new System.Windows.Controls.MenuItem
        {
            Header = "💰 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(balanceItem);

        var toppedUpItem = new System.Windows.Controls.MenuItem
        {
            Header = "🔁 Načítám...",
            IsEnabled = false
        };
        menu.Items.Add(toppedUpItem);

        var predictionItem = new System.Windows.Controls.MenuItem
        {
            Header = "📊 Načítám...",
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

        var dashboardItem = new System.Windows.Controls.MenuItem { Header = "📈 Dashboard" };
        dashboardItem.Click += (_, _) => OpenDashboard();
        menu.Items.Add(dashboardItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "⚙️ Nastavení" };
        settingsItem.Click += (_, _) => OpenSettings();
        menu.Items.Add(settingsItem);

        var refreshItem = new System.Windows.Controls.MenuItem { Header = "🔄 Obnovit teď" };
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
            PredictionItem = predictionItem,
            ErrorItem = errorItem
        };
    }

    public void UpdateTooltip(PollResult result)
    {
        if (_notifyIcon?.Tag is not TrayMenuRefs refs) return;

        var bal = result.Snapshot?.TotalBalanceDecimal ?? 0;
        var topped = result.Snapshot?.ToppedUpBalanceDecimal ?? 0;
        var pred = result.Prediction?.FormattedPrediction ?? "—";

        refs.BalanceItem.Header = $"💰 ${bal:F2} zbývá";
        refs.ToppedUpItem.Header = topped > 0
            ? $"🔁 Z toho ${topped:F2} vlastní"
            : "🔁 Všechno vlastní kredit";
        refs.PredictionItem.Header = $"📊 {pred}";

        refs.ErrorItem.Visibility = Visibility.Collapsed;

        _notifyIcon.ToolTipText = $"Zůstatek: ${bal:F2} | Predikce: {pred} | {DateTime.Now:HH:mm}";

        // Update tray icon with balance number
        _lastBalance = bal;
        UpdateIcon(bal);
    }

    private void UpdateIcon(decimal balance)
    {
        try
        {
            _notifyIcon!.Icon = CreateBalanceIcon(balance);
        }
        catch { }
    }

    private static System.Drawing.Icon CreateBalanceIcon(decimal balance)
    {
        // Zkusit vlastní ikonu z Resources
        if (balance == 0)
        {
            try
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;
                var customPath = Path.Combine(exeDir, "Resources", "app.ico");
                if (File.Exists(customPath)) return new System.Drawing.Icon(customPath);
            }
            catch { }
        }

        var text = balance > 0 ? FormatBalanceText(balance) : "$";
        var size = 32;
        var bitmap = new System.Drawing.Bitmap(size, size);
        try
        {
            using var g = System.Drawing.Graphics.FromImage(bitmap);
            g.Clear(System.Drawing.Color.FromArgb(0, 120, 215));
            var fontSize = text.Length <= 2 ? 18 : text.Length <= 3 ? 15 : 12;
            using var font = new System.Drawing.Font("Segoe UI", fontSize, System.Drawing.FontStyle.Bold);
            using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            var fmt = new System.Drawing.StringFormat
            {
                Alignment = System.Drawing.StringAlignment.Center,
                LineAlignment = System.Drawing.StringAlignment.Center
            };
            g.DrawString(text, font, brush, new System.Drawing.RectangleF(0, 0, size, size), fmt);

            // Klonování = vlastní handle, bezpečné pro TaskbarIcon
            using var temp = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            return (System.Drawing.Icon)temp.Clone();
        }
        finally
        {
            bitmap.Dispose();
        }
    }

    private static string FormatBalanceText(decimal balance)
    {
        if (balance >= 100) return ((int)balance).ToString();
        if (balance >= 10) return balance.ToString("F0", CultureInfo.InvariantCulture);
        if (balance >= 1) return balance.ToString("0.0", CultureInfo.InvariantCulture);
        return balance.ToString("0.00", CultureInfo.InvariantCulture);
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
        public System.Windows.Controls.MenuItem ErrorItem { get; set; } = null!;
    }
}
