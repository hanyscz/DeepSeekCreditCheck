using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IPollingService _polling;
    private readonly IBalanceRepository _balanceRepo;
    private readonly PredictionEngine _predictionEngine;
    private readonly IUpdateService _updateService;
    private bool _historyLoaded = false;

    private string _currentBalance = "—";
    private string _prediction = "—";
    private string _todaySpend = "—";
    private string _dailySpend = "—";
    private string _avgDailySpend = "—";
    private string _weeklySpend = "—";
    private string _monthlySpend = "—";
    private string _lastUpdated = "—";
    private bool _isLoading = false;
    private PlotModel? _spendPlot;
    private string _updateBannerText = "";
    private bool _isUpdateAvailable;
    private bool _isDownloadingUpdate;

    public string CurrentBalance { get => _currentBalance; set => SetProperty(ref _currentBalance, value); }
    public string TodaySpend { get => _todaySpend; set => SetProperty(ref _todaySpend, value); }
    public string Prediction { get => _prediction; set => SetProperty(ref _prediction, value); }
    public string DailySpend { get => _dailySpend; set => SetProperty(ref _dailySpend, value); }
    public string AvgDailySpend { get => _avgDailySpend; set => SetProperty(ref _avgDailySpend, value); }
    public string WeeklySpend { get => _weeklySpend; set => SetProperty(ref _weeklySpend, value); }
    public string MonthlySpend { get => _monthlySpend; set => SetProperty(ref _monthlySpend, value); }
    public string LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
    public string UpdateBannerText { get => _updateBannerText; set => SetProperty(ref _updateBannerText, value); }
    public bool IsUpdateAvailable { get => _isUpdateAvailable; set => SetProperty(ref _isUpdateAvailable, value); }
    public bool IsDownloadingUpdate { get => _isDownloadingUpdate; set => SetProperty(ref _isDownloadingUpdate, value); }

    public PlotModel? SpendPlot { get => _spendPlot; set => SetProperty(ref _spendPlot, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDataBrowserCommand { get; }
    public ICommand DownloadUpdateCommand { get; }

    private List<BalanceSnapshot> _history = new();

    public DashboardViewModel(IPollingService polling, IBalanceRepository balanceRepo, PredictionEngine predictionEngine, IUpdateService updateService)
    {
        _polling = polling;
        _balanceRepo = balanceRepo;
        _predictionEngine = predictionEngine;
        _updateService = updateService;
        RefreshCommand = new RelayCommand(async _ =>
        {
            IsLoading = true;
            if (_polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
            IsLoading = false;
        });
        OpenDataBrowserCommand = new RelayCommand(_ => OpenDataBrowser());
        DownloadUpdateCommand = new RelayCommand(async _ => await DownloadAndApplyUpdateAsync());

        RefreshUpdateInfo();

        _updateService.UpdateAvailable += _ =>
        {
            Application.Current.Dispatcher.BeginInvoke(() => RefreshUpdateInfo());
        };
    }

    public void RefreshUpdateInfo()
    {
        var loc = LocalizationService.Instance;
        if (_updateService.PendingRelease != null)
        {
            UpdateBannerText = loc.Format("update_available_menu", _updateService.PendingRelease.TagName);
            IsUpdateAvailable = true;
        }
        else
        {
            UpdateBannerText = "";
            IsUpdateAvailable = false;
        }
    }

    private async Task DownloadAndApplyUpdateAsync()
    {
        if (_updateService.PendingRelease == null || IsDownloadingUpdate) return;
        IsDownloadingUpdate = true;

        var loc = LocalizationService.Instance;
        try
        {
            UpdateBannerText = loc.Format("update_downloading", 0);

            var progress = new Progress<double>(p =>
            {
                var pct = (int)(p * 100);
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    UpdateBannerText = loc.Format("update_downloading", pct);
                });
            });

            var scriptPath = await _updateService.DownloadAndApplyAsync(progress, CancellationToken.None);

            UpdateBannerText = loc.Format("update_ready_menu", _updateService.PendingRelease.TagName);

            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c start \"\" /MIN \"{scriptPath}\"",
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = true
            };
            Process.Start(psi);

            Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
        }
        catch (Exception ex)
        {
            Logger.Error("Dashboard update failed", ex);
            UpdateBannerText = loc.Format("update_failed", ex.Message);
        }
        finally
        {
            IsDownloadingUpdate = false;
        }
    }

    public async Task LoadHistoryFromDbAsync()
    {
        try
        {
            var all = await _balanceRepo.GetAllAsync(limit: 10000);
            _history = all.OrderBy(h => h.Timestamp).ToList();
            _historyLoaded = true;
        }
        catch { }
    }

    private void OpenDataBrowser()
    {
        var window = new Windows.ViewDataWindow(_balanceRepo);
        window.ShowDialog();
    }

    public async Task OnPollCompleted(PollResult result)
    {
        if (!_historyLoaded)
            await LoadHistoryFromDbAsync();

        if (result.Snapshot != null)
            _history.Add(result.Snapshot);

        var cutoff = DateTime.UtcNow.AddDays(-180);
        _history.RemoveAll(h => h.Timestamp < cutoff);

        var bal = result.Snapshot?.TotalBalanceDecimal ?? 0;
        CurrentBalance = $"${bal:F2}";

        // Dnešní spotřeba
        TodaySpend = result.TodaySpend.HasValue
            ? $"${result.TodaySpend.Value:F2}"
            : "—";

        // Spočítat vlastní predikci a statistiky z historie
        var prediction = _predictionEngine.Calculate(_history, bal);
        Prediction = prediction.FormattedPrediction;
        AvgDailySpend = prediction.FormattedDailySpend;
        DailySpend = prediction.FormattedDailySpend;
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");

        UpdateSpendStats();
        BuildSpendChart();
    }

    private void UpdateSpendStats()
    {
        if (_history.Count < 2)
        {
            WeeklySpend = "—";
            MonthlySpend = "—";
            return;
        }

        // Používáme kalendářní dny — začátek dnešního dne (midnight local time)
        var todayStart = DateTime.Today; // local midnight, 00:00:00

        // Celková spotřeba za posledních 7 kalendářních dní (včetně dneška)
        var weekStart = todayStart.AddDays(-7).ToUniversalTime();
        var weekRecs = _history.Where(h => h.Timestamp >= weekStart).ToList();
        if (weekRecs.Count >= 2)
        {
            var weekSpend = SpendCalculator.SumPositiveDeltas(weekRecs);
            WeeklySpend = weekSpend > 0 ? $"${weekSpend:F2}" : "$0.00";
        }
        else WeeklySpend = "—";

        // Celková spotřeba za posledních 30 kalendářních dní
        var monthStart = todayStart.AddDays(-30).ToUniversalTime();
        var monthRecs = _history.Where(h => h.Timestamp >= monthStart).ToList();
        if (monthRecs.Count >= 2)
        {
            var monthSpend = SpendCalculator.SumPositiveDeltas(monthRecs);
            MonthlySpend = monthSpend > 0 ? $"${monthSpend:F2}" : "$0.00";
        }
        else MonthlySpend = "—";
    }

    private void BuildSpendChart()
    {
        var plot = new PlotModel
        {
            Title = "Spotřeba (USD/hodina)",
            TitleColor = OxyColors.White,
            PlotAreaBackground = OxyColor.FromRgb(30, 30, 30),
            Background = OxyColor.FromRgb(22, 22, 22),
            TextColor = OxyColor.FromRgb(200, 200, 200),
        };

        // Agregovat spotřebu po hodinách
        var sorted = _history.OrderBy(h => h.Timestamp).ToList();
        var byHour = new SortedDictionary<DateTime, decimal>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var spend = sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal;
            if (spend <= 0) continue;

            var t1 = sorted[i - 1].Timestamp;
            var t2 = sorted[i].Timestamp;
            var totalMinutes = (t2 - t1).TotalMinutes;
            if (totalMinutes <= 0) continue;

            var current = t1;
            while (current < t2)
            {
                var hourStart = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0, DateTimeKind.Utc);
                var hourEnd = hourStart.AddHours(1);
                var sliceStart = current > hourStart ? current : hourStart;
                var sliceEnd = t2 < hourEnd ? t2 : hourEnd;
                var minutesInSlice = (sliceEnd - sliceStart).TotalMinutes;
                if (minutesInSlice > 0)
                {
                    var proportion = minutesInSlice / totalMinutes;
                    var hourlySpend = spend * (decimal)proportion;
                    if (!byHour.ContainsKey(hourStart))
                        byHour[hourStart] = 0;
                    byHour[hourStart] += hourlySpend;
                }
                current = hourEnd;
            }
        }

        if (byHour.Count > 0)
        {
            var series = new LineSeries
            {
                Title = "USD/hod",
                Color = OxyColor.FromRgb(220, 80, 60),
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3
            };

            foreach (var kv in byHour)
                series.Points.Add(new DataPoint(
                    DateTimeAxis.ToDouble(kv.Key.ToLocalTime()),
                    (double)kv.Value));

            plot.Series.Add(series);

            if (sorted.Count > 0)
            {
                var firstDt = sorted.First().Timestamp.ToLocalTime();
                var lastDt = sorted.Last().Timestamp.ToLocalTime();
                var maxDt = lastDt.Date.AddDays(1).AddHours(6);
                var minVal = DateTimeAxis.ToDouble(firstDt.Date.AddHours(-2));
                var maxVal = DateTimeAxis.ToDouble(maxDt);

                plot.Axes.Add(new DateTimeAxis
                {
                    Position = AxisPosition.Bottom,
                    StringFormat = "dd.MM.\nHH:mm",
                    TextColor = OxyColor.FromRgb(160, 160, 160),
                    TicklineColor = OxyColor.FromRgb(60, 60, 60),
                    MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
                    MajorGridlineStyle = LineStyle.Dot,
                    Minimum = minVal,
                    Maximum = maxVal
                });
            }
        }

        plot.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot,
            Title = "USD",
            TitleColor = OxyColor.FromRgb(160, 160, 160)
        });

        SpendPlot = plot;
    }
}
