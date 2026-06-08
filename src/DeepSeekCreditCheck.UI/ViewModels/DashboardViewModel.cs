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
    private string _currentBalance = "—";
    private string _prediction = "—";
    private string _dailySpend = "—";
    private string _weeklySpend = "—";
    private string _monthlySpend = "—";
    private string _lastUpdated = "—";
    private PlotModel? _balancePlot;
    private PlotModel? _spendPlot;

    public string CurrentBalance { get => _currentBalance; set => SetProperty(ref _currentBalance, value); }
    public string Prediction { get => _prediction; set => SetProperty(ref _prediction, value); }
    public string DailySpend { get => _dailySpend; set => SetProperty(ref _dailySpend, value); }
    public string WeeklySpend { get => _weeklySpend; set => SetProperty(ref _weeklySpend, value); }
    public string MonthlySpend { get => _monthlySpend; set => SetProperty(ref _monthlySpend, value); }
    public string LastUpdated { get => _lastUpdated; set => SetProperty(ref _lastUpdated, value); }

    public PlotModel? BalancePlot { get => _balancePlot; set => SetProperty(ref _balancePlot, value); }
    public PlotModel? SpendPlot { get => _spendPlot; set => SetProperty(ref _spendPlot, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDataBrowserCommand { get; }

    private readonly List<BalanceSnapshot> _history = new();

    public DashboardViewModel(IPollingService polling, IBalanceRepository balanceRepo)
    {
        _polling = polling;
        _balanceRepo = balanceRepo;
        RefreshCommand = new RelayCommand(async _ =>
        {
            if (_polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        });
        OpenDataBrowserCommand = new RelayCommand(_ => OpenDataBrowser());
    }

    private void OpenDataBrowser()
    {
        var window = new Windows.ViewDataWindow(_balanceRepo);
        window.Owner = System.Windows.Application.Current.MainWindow;
        window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        window.ShowDialog();
    }

    public void OnPollCompleted(PollResult result)
    {
        if (result.Snapshot != null)
            _history.Add(result.Snapshot);

        var cutoff = DateTime.UtcNow.AddDays(-30);
        _history.RemoveAll(h => h.Timestamp < cutoff);

        CurrentBalance = $"${result.Snapshot?.TotalBalanceDecimal ?? 0:F2}";
        Prediction = result.Prediction?.FormattedPrediction ?? "—";
        DailySpend = result.Prediction?.FormattedDailySpend ?? "—";
        LastUpdated = DateTime.Now.ToString("HH:mm:ss");

        UpdateSpendStats();
        BuildBalanceChart();
        BuildSpendChart();
    }

    private void UpdateSpendStats()
    {
        var sorted = _history.OrderBy(h => h.Timestamp).ToList();
        var now = DateTime.UtcNow;

        var weekAgo = now.AddDays(-7);
        var weeklySpendList = sorted
            .Where(h => h.Timestamp >= weekAgo)
            .Select(h => h.TotalBalanceDecimal)
            .DefaultIfEmpty(0)
            .ToList();
        var weekTotal = weeklySpendList.Count >= 2
            ? weeklySpendList.First() - weeklySpendList.Last()
            : 0;
        WeeklySpend = weekTotal > 0 ? $"${weekTotal:F2}" : "—";

        var monthAgo = now.AddDays(-30);
        var monthlySpendList = sorted
            .Where(h => h.Timestamp >= monthAgo)
            .Select(h => h.TotalBalanceDecimal)
            .DefaultIfEmpty(0)
            .ToList();
        var monthTotal = monthlySpendList.Count >= 2
            ? monthlySpendList.First() - monthlySpendList.Last()
            : 0;
        MonthlySpend = monthTotal > 0 ? $"${monthTotal:F2}" : "—";
    }

    private void BuildBalanceChart()
    {
        var plot = new PlotModel
        {
            Title = "Zůstatek v čase",
            TitleColor = OxyColors.White,
            PlotAreaBackground = OxyColor.FromRgb(30, 30, 30),
            Background = OxyColor.FromRgb(22, 22, 22),
            TextColor = OxyColor.FromRgb(200, 200, 200)
        };

        var series = new LineSeries
        {
            Title = "USD",
            Color = OxyColor.FromRgb(0, 120, 215),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 3
        };

        var points = _history
            .OrderBy(h => h.Timestamp)
            .Select(h => new DataPoint(
                DateTimeAxis.ToDouble(h.Timestamp.ToLocalTime()),
                (double)h.TotalBalanceDecimal))
            .ToList();

        foreach (var p in points)
            series.Points.Add(p);

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
            Title = "Denní spotřeba (USD)",
            TitleColor = OxyColors.White,
            PlotAreaBackground = OxyColor.FromRgb(30, 30, 30),
            Background = OxyColor.FromRgb(22, 22, 22),
            TextColor = OxyColor.FromRgb(200, 200, 200)
        };

        var series = new BarSeries
        {
            FillColor = OxyColor.FromRgb(220, 80, 60),
            StrokeColor = OxyColor.FromRgb(180, 50, 30),
            StrokeThickness = 1
        };

        var sorted = _history.OrderBy(h => h.Timestamp).ToList();
        for (int i = 1; i < sorted.Count; i++)
        {
            var spend = sorted[i - 1].TotalBalanceDecimal - sorted[i].TotalBalanceDecimal;
            if (spend < 0) spend = 0;
            series.Items.Add(new BarItem { Value = (double)spend, CategoryIndex = i - 1 });
        }

        plot.Series.Add(series);
        plot.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColor.FromRgb(160, 160, 160),
            TicklineColor = OxyColor.FromRgb(60, 60, 60),
            MajorGridlineColor = OxyColor.FromRgb(40, 40, 40),
            MajorGridlineStyle = LineStyle.Dot
        });

        SpendPlot = plot;
    }
}
