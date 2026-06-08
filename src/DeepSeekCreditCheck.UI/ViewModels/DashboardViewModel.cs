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
    private bool _historyLoaded = false;

    private string _currentBalance = "—";
    private string _prediction = "—";
    private string _dailySpend = "—";
    private string _avgDailySpend = "—";
    private string _weeklySpend = "—";
    private string _monthlySpend = "—";
    private string _lastUpdated = "—";
    private PlotModel? _balancePlot;
    private PlotModel? _spendPlot;

    public string CurrentBalance { get => _currentBalance; set => SetProperty(ref _currentBalance, value); }
    public string Prediction { get => _prediction; set => SetProperty(ref _prediction, value); }
    public string DailySpend { get => _dailySpend; set => SetProperty(ref _dailySpend, value); }
    public string AvgDailySpend { get => _avgDailySpend; set => SetProperty(ref _avgDailySpend, value); }
    public string WeeklySpend { get => _weeklySpend; set => SetProperty(ref _weeklySpend, value); }
    public string MonthlySpend { get => _monthlySpend; set => SetProperty(ref _monthlySpend, value); }
    public string LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }

    public PlotModel? BalancePlot { get => _balancePlot; set => SetProperty(ref _balancePlot, value); }
    public PlotModel? SpendPlot { get => _spendPlot; set => SetProperty(ref _spendPlot, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDataBrowserCommand { get; }

    private List<BalanceSnapshot> _history = new();

    public DashboardViewModel(IPollingService polling, IBalanceRepository balanceRepo, PredictionEngine predictionEngine)
    {
        _polling = polling;
        _balanceRepo = balanceRepo;
        _predictionEngine = predictionEngine;
        RefreshCommand = new RelayCommand(async _ =>
        {
            if (_polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        });
        OpenDataBrowserCommand = new RelayCommand(_ => OpenDataBrowser());
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

        // Spočítat vlastní predikci a statistiky z historie
        var prediction = _predictionEngine.Calculate(_history, bal);
        Prediction = prediction.FormattedPrediction;
        AvgDailySpend = prediction.FormattedDailySpend;
        DailySpend = prediction.FormattedDailySpend;
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");

        UpdateSpendStats();
        BuildBalanceChart();
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
            var weekSpend = weekRecs.First().TotalBalanceDecimal - weekRecs.Last().TotalBalanceDecimal;
            WeeklySpend = weekSpend >= 0 ? $"${weekSpend:F2}" : "—";
        }
        else WeeklySpend = "—";

        // Celková spotřeba za posledních 30 kalendářních dní
        var monthStart = todayStart.AddDays(-30).ToUniversalTime();
        var monthRecs = _history.Where(h => h.Timestamp >= monthStart).ToList();
        if (monthRecs.Count >= 2)
        {
            var monthSpend = monthRecs.First().TotalBalanceDecimal - monthRecs.Last().TotalBalanceDecimal;
            MonthlySpend = monthSpend >= 0 ? $"${monthSpend:F2}" : "—";
        }
        else MonthlySpend = "—";
    }

    private void BuildBalanceChart()
    {
        var plot = new PlotModel
        {
            Title = "Zůstatek v čase (unikátní hodnoty)",
            TitleColor = OxyColors.White,
            PlotAreaBackground = OxyColor.FromRgb(30, 30, 30),
            Background = OxyColor.FromRgb(22, 22, 22),
            TextColor = OxyColor.FromRgb(200, 200, 200),
        };

        var series = new LineSeries
        {
            Title = "USD",
            Color = OxyColor.FromRgb(0, 120, 215),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3
        };

        // Jen unikátní hodnoty — pokud je stejný zůstatek, přidáme jen první výskyt
        decimal? lastValue = null;
        foreach (var h in _history.OrderBy(h => h.Timestamp))
        {
            if (lastValue.HasValue && h.TotalBalanceDecimal == lastValue.Value)
                continue; // přeskočit duplicitní hodnotu
            lastValue = h.TotalBalanceDecimal;
            series.Points.Add(new DataPoint(
                DateTimeAxis.ToDouble(h.Timestamp.ToLocalTime()),
                (double)h.TotalBalanceDecimal));
        }

        plot.Series.Add(series);
        plot.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd.MM.",
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot
        });
        plot.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot
        });

        BalancePlot = plot;
    }

    private void BuildSpendChart()
    {
        var plot = new PlotModel
        {
            Title = "Spotřeba za kalendářní den (USD)",
            TitleColor = OxyColors.White,
            PlotAreaBackground = OxyColor.FromRgb(30, 30, 30),
            Background = OxyColor.FromRgb(22, 22, 22),
            TextColor = OxyColor.FromRgb(200, 200, 200),
        };

        // Agregovat spotřebu po kalendářních dnech
        var sorted = _history.OrderBy(h => h.Timestamp).ToList();
        var byDay = new Dictionary<DateTime, decimal>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var spend = sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal;
            if (spend <= 0) continue; // dobití — přeskočit

            var day = sorted[i].Timestamp.Date; // kalendářní den (UTC)
            if (!byDay.ContainsKey(day))
                byDay[day] = 0;
            byDay[day] += spend;
        }

        var series = new LineSeries
        {
            Title = "USD/den",
            Color = OxyColor.FromRgb(220, 80, 60),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3
        };

        foreach (var kv in byDay.OrderBy(kv => kv.Key))
            series.Points.Add(new DataPoint(
                DateTimeAxis.ToDouble(kv.Key.ToLocalTime()),
                (double)kv.Value));

        plot.Series.Add(series);
        plot.Axes.Add(new DateTimeAxis
        {
            Position = AxisPosition.Bottom,
            StringFormat = "dd.MM.",
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot
        });
        plot.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot,
            Title = "USD/den",
            TitleColor = OxyColor.FromRgb(160, 160, 160)
        });

        SpendPlot = plot;
    }
}
