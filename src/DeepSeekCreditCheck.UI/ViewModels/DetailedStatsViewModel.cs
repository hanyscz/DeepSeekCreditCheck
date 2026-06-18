using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Legends;


namespace DeepSeekCreditCheck.UI.ViewModels;

public class DetailedStatsViewModel : BaseViewModel
{
    private readonly IUsageRepository _usageRepo;
    private readonly IDeepSeekPlatformClient _platformClient;
    private readonly IAppSettingsService _settings;

    private string _selectedMonth = "";
    private ObservableCollection<string> _availableMonths = new();
    private string _lastUpdatedText = "Data nejsou lokálně načtena";
    private bool _isLoading;
    private bool _hasLocalData;

    private PlotModel? _apiKeyChartModel;
    private PlotModel? _modelPieModel;
    private PlotModel? _dailyTrendModel;
    private PlotModel? _monthlyComparisonModel;

    private List<ApiKeyUsageItem> _apiKeyUsageList = new();
    private List<ModelUsageItem> _modelUsageList = new();
    private List<MonthlyComparisonItem> _monthlyComparisonList = new();

    public string SelectedMonth
    {
        get => _selectedMonth;
        set
        {
            if (SetProperty(ref _selectedMonth, value) && !string.IsNullOrEmpty(value))
            {
                _ = LoadLocalDataAsync();
            }
        }
    }

    public ObservableCollection<string> AvailableMonths
    {
        get => _availableMonths;
        set => SetProperty(ref _availableMonths, value);
    }

    public string LastUpdatedText
    {
        get => _lastUpdatedText;
        set => SetProperty(ref _lastUpdatedText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public bool HasLocalData
    {
        get => _hasLocalData;
        set
        {
            if (SetProperty(ref _hasLocalData, value))
            {
                OnPropertyChanged(nameof(NoLocalData));
            }
        }
    }

    public bool NoLocalData => !HasLocalData;

    public PlotModel? ApiKeyChartModel { get => _apiKeyChartModel; set => SetProperty(ref _apiKeyChartModel, value); }
    public PlotModel? ModelPieModel { get => _modelPieModel; set => SetProperty(ref _modelPieModel, value); }
    public PlotModel? DailyTrendModel { get => _dailyTrendModel; set => SetProperty(ref _dailyTrendModel, value); }
    public PlotModel? MonthlyComparisonModel { get => _monthlyComparisonModel; set => SetProperty(ref _monthlyComparisonModel, value); }

    public List<ApiKeyUsageItem> ApiKeyUsageList { get => _apiKeyUsageList; set => SetProperty(ref _apiKeyUsageList, value); }
    public List<ModelUsageItem> ModelUsageList { get => _modelUsageList; set => SetProperty(ref _modelUsageList, value); }
    public List<MonthlyComparisonItem> MonthlyComparisonList { get => _monthlyComparisonList; set => SetProperty(ref _monthlyComparisonList, value); }

    public ICommand DownloadStatsCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }

    public DetailedStatsViewModel(IUsageRepository usageRepo, IDeepSeekPlatformClient platformClient, IAppSettingsService settings)
    {
        _usageRepo = usageRepo;
        _platformClient = platformClient;
        _settings = settings;

        DownloadStatsCommand = new RelayCommand(async _ => await DownloadStatsAsync());
        PreviousMonthCommand = new RelayCommand(_ => GoToPreviousMonth());
        NextMonthCommand = new RelayCommand(_ => GoToNextMonth(), _ => CanGoToNextMonth());

        // Předvyplnit dostupné měsíce
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await RefreshAvailableMonthsAsync();

            // Nastavit výchozí měsíc na aktuální
            var currentMonthStr = DateTime.Today.ToString("yyyy-MM");
            if (!AvailableMonths.Contains(currentMonthStr))
            {
                AvailableMonths.Insert(0, currentMonthStr);
            }
            SelectedMonth = currentMonthStr;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chyba při inicializaci statistik: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshAvailableMonthsAsync()
    {
        var totals = await _usageRepo.GetMonthlyTotalsAsync();
        var currentSelected = SelectedMonth;

        var list = totals
            .Select(t => $"{t.Year}-{t.Month:D2}")
            .ToList();

        var todayStr = DateTime.Today.ToString("yyyy-MM");
        if (!list.Contains(todayStr))
        {
            list.Add(todayStr);
        }

        var sorted = list.OrderByDescending(x => x).ToList();

        AvailableMonths.Clear();
        foreach (var m in sorted)
        {
            AvailableMonths.Add(m);
        }

        if (!string.IsNullOrEmpty(currentSelected) && AvailableMonths.Contains(currentSelected))
        {
            SelectedMonth = currentSelected;
        }
    }

    private async Task LoadLocalDataAsync()
    {
        if (string.IsNullOrEmpty(SelectedMonth)) return;

        var parts = SelectedMonth.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            return;
        }

        IsLoading = true;
        try
        {
            var details = await _usageRepo.GetUsageDetailsAsync(year, month);
            if (details == null || details.Count == 0)
            {
                HasLocalData = false;
                LastUpdatedText = "Pro tento měsíc nejsou stažena lokální data.";
                ClearCharts();
                return;
            }

            HasLocalData = true;

            // Načíst čas poslední aktualizace z nastavení
            var updateKey = $"PlatformUsageUpdated_{year}_{month:D2}";
            var lastUpdateVal = await _settings.GetAsync(updateKey);
            if (DateTime.TryParse(lastUpdateVal, null, DateTimeStyles.RoundtripKind, out var dt))
            {
                LastUpdatedText = $"Zobrazeno z lokální paměti. Aktualizováno: {dt.ToLocalTime():g}";
            }
            else
            {
                LastUpdatedText = "Zobrazeno z lokální paměti.";
            }

            // Zpracovat data pro UI a grafy
            ProcessData(details, year, month);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chyba při načítání lokálních dat: {ex.Message}", "Chyba", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearCharts()
    {
        ApiKeyUsageList = new List<ApiKeyUsageItem>();
        ModelUsageList = new List<ModelUsageItem>();
        ApiKeyChartModel = null;
        ModelPieModel = null;
        DailyTrendModel = null;
        MonthlyComparisonModel = null;
    }

    private async Task DownloadStatsAsync()
    {
        if (string.IsNullOrEmpty(SelectedMonth)) return;

        var parts = SelectedMonth.Split('-');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var year) || !int.TryParse(parts[1], out var month))
        {
            return;
        }

        var token = await _settings.GetSessionTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            MessageBox.Show("Nejprve se musíte na hlavní obrazovce propojit s platformou DeepSeek.", "Připojení k platformě", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            // 1. Stáhnout ZIP
            var zipBytes = await _platformClient.GetUsageExportZipAsync(token, year, month);

            // 2. Naparsovat ZIP/CSV
            var details = UsageCsvParser.ParseZip(zipBytes, year, month);

            // 3. Uložit do DB
            await _usageRepo.SaveUsageDetailsAsync(year, month, details);

            // 4. Uložit čas aktualizace
            var updateKey = $"PlatformUsageUpdated_{year}_{month:D2}";
            await _settings.SetAsync(updateKey, DateTime.UtcNow.ToString("o"));

            // 5. Obnovit seznam dostupných měsíců a načíst data
            await RefreshAvailableMonthsAsync();
            await LoadLocalDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Chyba při stahování dat z platformy: {ex.Message}", "Chyba stahování", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ProcessData(IReadOnlyList<UsageDetailSnapshot> details, int year, int month)
    {
        // 1. Zpracování pro záložku API klíčů
        var apiKeyGroups = details.GroupBy(d => new { d.ApiKeyName, d.Model });
        var apiKeysList = new List<ApiKeyUsageItem>();
        foreach (var g in apiKeyGroups)
        {
            var item = new ApiKeyUsageItem
            {
                ApiKeyName = g.Key.ApiKeyName,
                Model = g.Key.Model,
                ApiKeyMasked = g.FirstOrDefault()?.ApiKeyMasked ?? "",
                RequestCount = g.Where(x => x.Type == "request_count").Sum(x => x.Amount),
                CacheHitTokens = g.Where(x => x.Type == "input_cache_hit_tokens").Sum(x => x.Amount),
                CacheMissTokens = g.Where(x => x.Type == "input_cache_miss_tokens").Sum(x => x.Amount),
                OutputTokens = g.Where(x => x.Type == "output_tokens").Sum(x => x.Amount)
            };

            // Vypočíst celkové náklady pro tento klíč a model
            double cost = 0;
            foreach (var r in g)
            {
                if (r.Type != "request_count" && r.Price.HasValue)
                {
                    cost += r.Price.Value * r.Amount;
                }
            }
            item.Cost = cost;
            apiKeysList.Add(item);
        }
        ApiKeyUsageList = apiKeysList.OrderByDescending(x => x.Cost).ToList();

        // 2. Zpracování pro záložku Modelů
        var modelGroups = details.GroupBy(d => d.Model);
        var modelsList = new List<ModelUsageItem>();
        foreach (var g in modelGroups)
        {
            var item = new ModelUsageItem
            {
                Model = g.Key,
                RequestCount = g.Where(x => x.Type == "request_count").Sum(x => x.Amount),
                CacheHitTokens = g.Where(x => x.Type == "input_cache_hit_tokens").Sum(x => x.Amount),
                CacheMissTokens = g.Where(x => x.Type == "input_cache_miss_tokens").Sum(x => x.Amount),
                OutputTokens = g.Where(x => x.Type == "output_tokens").Sum(x => x.Amount)
            };

            double cost = 0;
            foreach (var r in g)
            {
                if (r.Type != "request_count" && r.Price.HasValue)
                {
                    cost += r.Price.Value * r.Amount;
                }
            }
            item.Cost = cost;
            modelsList.Add(item);
        }
        ModelUsageList = modelsList.OrderByDescending(x => x.Cost).ToList();

        // 3. Sloupcový graf API klíčů (kdo vede)
        var apiKeyChart = CreateDarkPlotModel("Náklady podle API klíčů (USD)");
        var itemsWithCost = ApiKeyUsageList.Where(x => x.Cost > 0).ToList();
        // Obrátíme pořadí, aby největší náklady byly v grafu nahoře (kategorie v CategoryAxis se vykreslují zdola nahoru)
        var chartItems = itemsWithCost.AsEnumerable().Reverse().ToList();
        
        if (chartItems.Count > 0)
        {
            var yAxis = new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "ApiKeyCategoryAxis",
                TextColor = OxyColor.Parse("#E0E0E0"),
                TicklineColor = OxyColor.Parse("#444444")
            };
            foreach (var item in chartItems)
            {
                yAxis.Labels.Add($"{item.ApiKeyName} ({item.Model})");
            }
            apiKeyChart.Axes.Add(yAxis);

            var xAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Key = "ApiKeyPriceAxis",
                TextColor = OxyColor.Parse("#E0E0E0"),
                TicklineColor = OxyColor.Parse("#444444"),
                MajorGridlineStyle = LineStyle.Dot,
                MajorGridlineColor = OxyColor.Parse("#2A2A2A"),
                StringFormat = "$0.00",
                MinimumPadding = 0,
                MaximumPadding = 0.15
            };
            apiKeyChart.Axes.Add(xAxis);

            var barSeries = new BarSeries
            {
                XAxisKey = "ApiKeyPriceAxis",
                YAxisKey = "ApiKeyCategoryAxis",
                TextColor = OxyColor.Parse("#E0E0E0"),
                LabelPlacement = LabelPlacement.Outside,
                LabelFormatString = "${0:F2}",
                FillColor = OxyColor.Parse("#4FC3F7")
            };
            foreach (var item in chartItems)
            {
                barSeries.Items.Add(new BarItem { Value = item.Cost });
            }
            apiKeyChart.Series.Add(barSeries);
        }
        ApiKeyChartModel = apiKeyChart;

        // 4. Koláčový graf modelů
        var modelPie = CreateDarkPlotModel("Podíl nákladů podle modelů");
        var mdlPieSeries = new PieSeries { StrokeThickness = 1.0, InsideLabelPosition = 0.5, AngleSpan = 360, StartAngle = 0, InsideLabelFormat = "{1}: {2:F2}%" };
        foreach (var item in ModelUsageList.Where(x => x.Cost > 0))
        {
            mdlPieSeries.Slices.Add(new PieSlice(item.Model, item.Cost));
        }
        if (mdlPieSeries.Slices.Count > 0)
        {
            modelPie.Series.Add(mdlPieSeries);
        }
        ModelPieModel = modelPie;

        // 5. Denní trend
        BuildDailyTrendChart(details, year, month);

        // 6. Meziměsíční porovnání
        _ = BuildMonthlyComparisonChartAsync();
    }

    private void BuildDailyTrendChart(IReadOnlyList<UsageDetailSnapshot> details, int year, int month)
    {
        var trend = CreateDarkPlotModel("Denní vývoj nákladů (USD)");

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var xAxis = new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            TextColor = OxyColor.Parse("#E0E0E0"),
            TicklineColor = OxyColor.Parse("#444444"),
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.Parse("#2A2A2A")
        };
        for (int d = 1; d <= daysInMonth; d++)
        {
            xAxis.Labels.Add(d.ToString());
        }
        trend.Axes.Add(xAxis);

        var yAxis = new LinearAxis
        {
            Position = AxisPosition.Left,
            TextColor = OxyColor.Parse("#E0E0E0"),
            TicklineColor = OxyColor.Parse("#444444"),
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.Parse("#2A2A2A"),
            StringFormat = "$0.00"
        };
        trend.Axes.Add(yAxis);

        var apiKeys = details.Select(x => x.ApiKeyName).Distinct().ToList();
        foreach (var key in apiKeys)
        {
            var series = new LineSeries
            {
                Title = key,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3,
                StrokeThickness = 2
            };

            var keyData = details.Where(x => x.ApiKeyName == key).ToList();
            for (int day = 1; day <= daysInMonth; day++)
            {
                var dayStr = $"{year}-{month:D2}-{day:D2}";
                double cost = 0;
                foreach (var r in keyData)
                {
                    if (r.UtcDate == dayStr && r.Type != "request_count" && r.Price.HasValue)
                    {
                        cost += r.Price.Value * r.Amount;
                    }
                }
                series.Points.Add(new DataPoint(day - 1, cost));
            }
            trend.Series.Add(series);
        }

        DailyTrendModel = trend;
    }

    private async Task BuildMonthlyComparisonChartAsync()
    {
        var comp = CreateDarkPlotModel("Porovnání celkových nákladů po měsících (USD)");
        var totals = await _usageRepo.GetMonthlyTotalsAsync();

        if (totals == null || totals.Count == 0) return;

        var sorted = totals.OrderBy(t => t.Year).ThenBy(t => t.Month).ToList();

        var yAxis = new CategoryAxis
        {
            Position = AxisPosition.Left,
            Key = "CategoryAxis",
            TextColor = OxyColor.Parse("#E0E0E0"),
            TicklineColor = OxyColor.Parse("#444444")
        };
        foreach (var t in sorted)
        {
            yAxis.Labels.Add($"{t.Year}-{t.Month:D2}");
        }
        comp.Axes.Add(yAxis);

        var xAxis = new LinearAxis
        {
            Position = AxisPosition.Bottom,
            Key = "ValueAxis",
            TextColor = OxyColor.Parse("#E0E0E0"),
            TicklineColor = OxyColor.Parse("#444444"),
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.Parse("#2A2A2A"),
            StringFormat = "$0.00",
            MinimumPadding = 0,
            MaximumPadding = 0.15
        };
        comp.Axes.Add(xAxis);

        var barSeries = new BarSeries
        {
            XAxisKey = "ValueAxis",
            YAxisKey = "CategoryAxis",
            TextColor = OxyColor.Parse("#E0E0E0"),
            LabelPlacement = LabelPlacement.Outside,
            LabelFormatString = "${0:F2}",
            FillColor = OxyColor.Parse("#4FC3F7")
        };
        foreach (var t in sorted)
        {
            barSeries.Items.Add(new BarItem { Value = t.TotalCost });
        }
        comp.Series.Add(barSeries);

        MonthlyComparisonModel = comp;

        // Naplnit tabulku porovnání měsíců
        MonthlyComparisonList = sorted.Select(t => new MonthlyComparisonItem
        {
            Month = $"{t.Year}-{t.Month:D2}",
            RequestCount = t.RequestCount,
            CacheHitTokens = t.CacheHitTokens,
            CacheMissTokens = t.CacheMissTokens,
            OutputTokens = t.OutputTokens,
            Cost = t.TotalCost
        }).OrderByDescending(x => x.Month).ToList();
    }

    private void GoToPreviousMonth()
    {
        if (DateTime.TryParseExact(SelectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            SelectedMonth = dt.AddMonths(-1).ToString("yyyy-MM");
        }
    }

    private void GoToNextMonth()
    {
        if (CanGoToNextMonth())
        {
            if (DateTime.TryParseExact(SelectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                SelectedMonth = dt.AddMonths(1).ToString("yyyy-MM");
            }
        }
    }

    private bool CanGoToNextMonth()
    {
        if (DateTime.TryParseExact(SelectedMonth, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            var today = DateTime.Today;
            return dt.Year < today.Year || (dt.Year == today.Year && dt.Month < today.Month);
        }
        return false;
    }

    private PlotModel CreateDarkPlotModel(string title)
    {
        var model = new PlotModel
        {
            Title = title,
            Background = OxyColor.Parse("#252525"),
            TextColor = OxyColor.Parse("#E0E0E0"),
            PlotAreaBorderColor = OxyColor.Parse("#444444"),
            TitleColor = OxyColor.Parse("#E0E0E0")
        };

        var legend = new Legend
        {
            LegendTextColor = OxyColor.Parse("#B0BEC5"),
            LegendPosition = LegendPosition.TopRight,
            LegendBackground = OxyColor.Parse("#202020"),
            LegendBorder = OxyColor.Parse("#3A3A3A")
        };
        model.Legends.Add(legend);

        return model;
    }
}

public class ApiKeyUsageItem
{
    public string ApiKeyName { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKeyMasked { get; set; } = "";
    public long RequestCount { get; set; }
    public long CacheHitTokens { get; set; }
    public long CacheMissTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens => CacheHitTokens + CacheMissTokens + OutputTokens;
    public double CacheRatio => TotalTokens > 0 ? (double)CacheHitTokens / TotalTokens : 0;
    public string CacheRatioPercent => $"{CacheRatio * 100:F1} %";
    public double Cost { get; set; }
    public string CostText => $"$ {Cost:F2}";

    // Naformátovaný text s oddělovači tisíců
    public string RequestCountText => RequestCount.ToString("N0");
    public string CacheHitTokensText => CacheHitTokens.ToString("N0");
    public string CacheMissTokensText => CacheMissTokens.ToString("N0");
    public string OutputTokensText => OutputTokens.ToString("N0");
    public string TotalTokensText => TotalTokens.ToString("N0");
}

public class ModelUsageItem
{
    public string Model { get; set; } = "";
    public long RequestCount { get; set; }
    public long CacheHitTokens { get; set; }
    public long CacheMissTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens => CacheHitTokens + CacheMissTokens + OutputTokens;
    public double Cost { get; set; }
    public string CostText => $"$ {Cost:F2}";

    // Naformátovaný text s oddělovači tisíců
    public string RequestCountText => RequestCount.ToString("N0");
    public string CacheHitTokensText => CacheHitTokens.ToString("N0");
    public string CacheMissTokensText => CacheMissTokens.ToString("N0");
    public string OutputTokensText => OutputTokens.ToString("N0");
    public string TotalTokensText => TotalTokens.ToString("N0");
}

public class MonthlyComparisonItem
{
    public string Month { get; set; } = "";
    public long RequestCount { get; set; }
    public long CacheHitTokens { get; set; }
    public long CacheMissTokens { get; set; }
    public long OutputTokens { get; set; }
    public long TotalTokens => CacheHitTokens + CacheMissTokens + OutputTokens;
    public double Cost { get; set; }
    public string CostText => $"$ {Cost:F2}";

    // Naformátovaný text s oddělovači tisíců
    public string RequestCountText => RequestCount.ToString("N0");
    public string CacheHitTokensText => CacheHitTokens.ToString("N0");
    public string CacheMissTokensText => CacheMissTokens.ToString("N0");
    public string OutputTokensText => OutputTokens.ToString("N0");
    public string TotalTokensText => TotalTokens.ToString("N0");
}
