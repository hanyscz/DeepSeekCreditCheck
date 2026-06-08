using System.Windows.Input;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;

namespace DeepSeekCreditCheck.UI.ViewModels;

public class DashboardViewModel : BaseViewModel
{
    private readonly IPollingService _polling;
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

    private readonly List<BalanceSnapshot> _history = new();

    public DashboardViewModel(IPollingService polling)
    {
        _polling = polling;
        RefreshCommand = new RelayCommand(async _ =>
        {
            if (_polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
        });
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

        // Týdenní spotřeba
        var weekAgo = now.AddDays(-7);
        var weeklySpend = sorted
            .Where(h => h.Timestamp >= weekAgo)
            .Select(h => h.TotalBalanceDecimal)
            .DefaultIfEmpty(0)
            .ToList();
        var weekTotal = weeklySpend.Count >= 2
            ? weeklySpend.First() - weeklySpend.Last()
            : 0;
        WeeklySpend = weekTotal > 0 ? $"${weekTotal:F2}" : "—";

        // Měsíční spotřeba
        var monthAgo = now.AddDays(-30);
        var monthlySpend = sorted
            .Where(h => h.Timestamp >= monthAgo)
            .Select(h => h.TotalBalanceDecimal)
            .DefaultIfEmpty(0)
            .ToList();
        var monthTotal = monthlySpend.Count >= 2
            ? monthlySpend.First() - monthlySpend.Last()
            : 0;
        MonthlySpend = monthTotal > 0 ? $"${monthTotal:F2}" : "—";
    }

    private void BuildBalanceChart()
    {
        var plot = new PlotModel { Title = "Zůstatek v čase" };
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
            StringFormat = "dd.MM."
        });

        BalancePlot = plot;
    }

    private void BuildSpendChart()
    {
        var plot = new PlotModel { Title = "Denní spotřeba (USD)" };
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
            series.Items.Add(new BarItem
            {
                Value = (double)spend,
                CategoryIndex = i - 1
            });
        }

        plot.Series.Add(series);
        SpendPlot = plot;
    }
}
