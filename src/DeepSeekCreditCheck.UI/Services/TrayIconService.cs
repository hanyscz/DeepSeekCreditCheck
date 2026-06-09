using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Hardcodet.Wpf.TaskbarNotification;
using DeepSeekCreditCheck.Core.Models;
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
    private MenuItem? _updateCheckItem;
    private MenuItem? _updateActionItem;
    private bool _isUpdating;
    private CancellationTokenSource? _periodicTimerCts;

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

        var todaySpendItem = new System.Windows.Controls.MenuItem
        {
            Header = "📉 " + loc["loading"],
            IsEnabled = false
        };
        menu.Items.Add(todaySpendItem);

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

        // Update check (always visible)
        _updateCheckItem = new MenuItem { Header = loc["tray_check_updates"] };
        _updateCheckItem.Click += async (_, _) =>
        {
            _updateCheckItem.IsEnabled = false;
            await CheckForUpdatesAsync();
            _updateCheckItem.IsEnabled = true;
        };
        menu.Items.Add(_updateCheckItem);

        // Update action (hidden until update found)
        _updateActionItem = new MenuItem
        {
            Header = "",
            Visibility = Visibility.Collapsed
        };
        _updateActionItem.Click += async (_, _) =>
        {
            if (_isUpdating) return;
            await DownloadAndApplyUpdateAsync();
        };
        menu.Items.Add(_updateActionItem);

        menu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = loc["tray_exit"] };
        exitItem.Click += (_, _) => Application.Current.Shutdown();
        menu.Items.Add(exitItem);

        _notifyIcon.ContextMenu = menu;
        _notifyIcon.TrayMouseDoubleClick += (_, _) => OpenDashboard();

        _notifyIcon.Tag = new TrayMenuRefs
        {
            BalanceItem = balanceItem,
            TodaySpendItem = todaySpendItem,
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
        var todayStr = result.TodaySpend.HasValue ? $"${result.TodaySpend.Value:F2}" : "—";

        refs.BalanceItem.Header = loc.Format("tray_balance", balStr);
        refs.TodaySpendItem.Header = loc.Format("tray_today", todayStr);
        refs.PredictionItem.Header = loc.Format("tray_prediction", pred);

        refs.ErrorItem.Visibility = Visibility.Collapsed;

        // Custom tooltip — 2x větší
        if (_tooltipText != null)
        {
            _tooltipText.Text = $"{loc["tooltip_title"]}\n" +
                $"━━━━━━━━━━━━━━━━━━\n" +
                $"{loc.Format("tooltip_balance", balStr)}\n" +
                $"{loc.Format("tooltip_today", todayStr)}\n" +
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

    public void ShowNotification(string message, string? actionText = null, Action? action = null)
    {
        if (actionText != null && action != null)
            NotificationToast.Show(message, actionText, action);
        else
            NotificationToast.Show(message);
    }

    private void OpenDashboard()
    {
        if (_dashboardWindow == null || !_dashboardWindow.IsLoaded)
        {
            var vm = _services.GetRequiredService<DashboardViewModel>();
            vm.RefreshUpdateInfo();
            _dashboardWindow = new DashboardWindow(vm);
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

    public async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            var updateService = _services.GetRequiredService<IUpdateService>();
            var release = await updateService.CheckForUpdateAsync(CancellationToken.None);

            if (release != null)
            {
                updateService.MarkUpdateAvailable(release);
                var loc = LocalizationService.Instance;
                var menuLabel = loc.Format("update_available_menu", release.TagName);
                _updateActionItem.Header = menuLabel;
                _updateActionItem.Visibility = Visibility.Visible;
                _updateActionItem.IsEnabled = true;

                ShowNotification(
                    loc.Format("update_available_notify", release.TagName),
                    menuLabel,
                    async () => await DownloadAndApplyUpdateAsync());
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Startup update check failed", ex);
        }
        finally
        {
            StartPeriodicUpdateChecks();
        }
    }

    private void StartPeriodicUpdateChecks()
    {
        _periodicTimerCts?.Cancel();
        _periodicTimerCts = new CancellationTokenSource();
        var ct = _periodicTimerCts.Token;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(4));
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    var updateService = _services.GetRequiredService<IUpdateService>();
                    var release = await updateService.CheckForUpdateAsync(ct);

                    if (release != null)
                    {
                        updateService.MarkUpdateAvailable(release);
                        var loc = LocalizationService.Instance;
                        var menuLabel = loc.Format("update_available_menu", release.TagName);

                        await Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            _updateActionItem!.Header = menuLabel;
                            _updateActionItem.Visibility = Visibility.Visible;
                            _updateActionItem.IsEnabled = true;
                            ShowNotification(
                                loc.Format("update_available_notify", release.TagName),
                                menuLabel,
                                async () => await DownloadAndApplyUpdateAsync());
                        });
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Logger.Error("Periodic update check failed", ex);
                }
            }
        }, ct);
    }

    private async Task CheckForUpdatesAsync()
    {
        var loc = LocalizationService.Instance;
        try
        {
            var updateService = _services.GetRequiredService<IUpdateService>();
            _updateCheckItem.Header = loc["tray_check_updates"] + "...";
            _updateCheckItem.IsEnabled = false;

            var release = await updateService.CheckForUpdateAsync(CancellationToken.None);

            if (release != null)
            {
                updateService.MarkUpdateAvailable(release);
                var menuLabel = loc.Format("update_available_menu", release.TagName);
                _updateActionItem.Header = menuLabel;
                _updateActionItem.Visibility = Visibility.Visible;
                _updateActionItem.IsEnabled = true;
                ShowNotification(loc.Format("update_available_notify", release.TagName), menuLabel,
                    async () => await DownloadAndApplyUpdateAsync());
            }
            else
            {
                ShowNotification(loc["update_up_to_date"]);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Manual update check failed", ex);
            ShowNotification(loc.Format("update_check_failed", ex.Message));
        }
        finally
        {
            _updateCheckItem.Header = loc["tray_check_updates"];
            _updateCheckItem.IsEnabled = true;
        }
    }

    private async Task DownloadAndApplyUpdateAsync()
    {
        var updateService = _services.GetRequiredService<IUpdateService>();
        if (updateService.PendingRelease == null) return;
        _isUpdating = true;

        var loc = LocalizationService.Instance;
        try
        {
            _updateActionItem.Header = loc.Format("update_downloading", 0);
            _updateActionItem.IsEnabled = false;

            var progress = new Progress<double>(p =>
            {
                var pct = (int)(p * 100);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    _updateActionItem.Header = loc.Format("update_downloading", pct);
                });
            });

            var scriptPath = await updateService.DownloadAndApplyAsync(progress, CancellationToken.None);

            _updateActionItem.Header = loc.Format("update_ready_menu", updateService.PendingRelease.TagName);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" /MIN \"{scriptPath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);

            _notifyIcon?.Dispose();
            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            Logger.Error("Download/apply update failed", ex);
            ShowNotification(loc.Format("update_failed", ex.Message));
            var tag = updateService.PendingRelease?.TagName ?? "";
            _updateActionItem.Header = loc.Format("update_retry", tag);
            _updateActionItem.IsEnabled = true;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    public void Dispose()
    {
        _periodicTimerCts?.Cancel();
        _periodicTimerCts?.Dispose();
        _notifyIcon?.Dispose();
    }

    private class TrayMenuRefs
    {
        public System.Windows.Controls.MenuItem BalanceItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem TodaySpendItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem PredictionItem { get; set; } = null!;
        public System.Windows.Controls.MenuItem ErrorItem { get; set; } = null!;
    }
}
