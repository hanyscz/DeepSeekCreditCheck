using System.Diagnostics;
using System.Text.Json.Nodes;
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
    private readonly IDeepSeekPlatformClient _platformClient;
    private readonly IAppSettingsService _settings;
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

    private bool _isPlatformConnected;
    private string _platformTotalTokens = "—";
    private string _platformCost = "—";
    private string _platformCacheRatio = "—";
    private string _platformHeading = "—";

    private string _platformProInput = "—";
    private string _platformProCache = "—";
    private string _platformProOutput = "—";
    private string _platformProTotal = "—";
    private string _platformProCost = "—";

    private string _platformFlashInput = "—";
    private string _platformFlashCache = "—";
    private string _platformFlashOutput = "—";
    private string _platformFlashTotal = "—";
    private string _platformFlashCost = "—";

    private string _platformTotalInput = "—";
    private string _platformTotalCache = "—";
    private string _platformTotalOutput = "—";
    private string _platformTotalTotal = "—";
    private string _platformTotalCost = "—";

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

    public bool IsPlatformConnected 
    { 
        get => _isPlatformConnected; 
        set 
        { 
            if (SetProperty(ref _isPlatformConnected, value))
            {
                OnPropertyChanged(nameof(IsPlatformDisconnected));
            }
        } 
    }
    public bool IsPlatformDisconnected => !IsPlatformConnected;

    public string PlatformTotalTokens { get => _platformTotalTokens; set => SetProperty(ref _platformTotalTokens, value); }
    public string PlatformCost { get => _platformCost; set => SetProperty(ref _platformCost, value); }
    public string PlatformCacheRatio { get => _platformCacheRatio; set => SetProperty(ref _platformCacheRatio, value); }
    public string PlatformHeading { get => _platformHeading; set => SetProperty(ref _platformHeading, value); }

    public string PlatformProInput { get => _platformProInput; set => SetProperty(ref _platformProInput, value); }
    public string PlatformProCache { get => _platformProCache; set => SetProperty(ref _platformProCache, value); }
    public string PlatformProOutput { get => _platformProOutput; set => SetProperty(ref _platformProOutput, value); }
    public string PlatformProTotal { get => _platformProTotal; set => SetProperty(ref _platformProTotal, value); }
    public string PlatformProCost { get => _platformProCost; set => SetProperty(ref _platformProCost, value); }

    public string PlatformFlashInput { get => _platformFlashInput; set => SetProperty(ref _platformFlashInput, value); }
    public string PlatformFlashCache { get => _platformFlashCache; set => SetProperty(ref _platformFlashCache, value); }
    public string PlatformFlashOutput { get => _platformFlashOutput; set => SetProperty(ref _platformFlashOutput, value); }
    public string PlatformFlashTotal { get => _platformFlashTotal; set => SetProperty(ref _platformFlashTotal, value); }
    public string PlatformFlashCost { get => _platformFlashCost; set => SetProperty(ref _platformFlashCost, value); }

    public string PlatformTotalInput { get => _platformTotalInput; set => SetProperty(ref _platformTotalInput, value); }
    public string PlatformTotalCache { get => _platformTotalCache; set => SetProperty(ref _platformTotalCache, value); }
    public string PlatformTotalOutput { get => _platformTotalOutput; set => SetProperty(ref _platformTotalOutput, value); }
    public string PlatformTotalTotal { get => _platformTotalTotal; set => SetProperty(ref _platformTotalTotal, value); }
    public string PlatformTotalCost { get => _platformTotalCost; set => SetProperty(ref _platformTotalCost, value); }

    public PlotModel? SpendPlot { get => _spendPlot; set => SetProperty(ref _spendPlot, value); }

    public ICommand RefreshCommand { get; }
    public ICommand OpenDataBrowserCommand { get; }
    public ICommand DownloadUpdateCommand { get; }
    public ICommand LoginPlatformCommand { get; }
    public ICommand LogoutPlatformCommand { get; }
    public ICommand PreviousMonthCommand { get; }
    public ICommand NextMonthCommand { get; }
    public ICommand OpenDetailedStatsCommand { get; }

    private readonly IUsageRepository _usageRepo;
    private DateTime _platformSelectedMonth = DateTime.Today;
    private List<BalanceSnapshot> _history = new();

    public DashboardViewModel(IPollingService polling, IBalanceRepository balanceRepo, PredictionEngine predictionEngine, IUpdateService updateService, IDeepSeekPlatformClient platformClient, IAppSettingsService settings, IUsageRepository usageRepo)
    {
        _polling = polling;
        _balanceRepo = balanceRepo;
        _predictionEngine = predictionEngine;
        _updateService = updateService;
        _platformClient = platformClient;
        _settings = settings;
        _usageRepo = usageRepo;
        RefreshCommand = new RelayCommand(async _ =>
        {
            IsLoading = true;
            if (_polling is PollingService ps)
                await ps.PollOnceAsync(CancellationToken.None);
            await LoadPlatformStatsAsync();
            IsLoading = false;
        });
        OpenDataBrowserCommand = new RelayCommand(_ => OpenDataBrowser());
        DownloadUpdateCommand = new RelayCommand(async _ => await DownloadAndApplyUpdateAsync());
        LoginPlatformCommand = new RelayCommand(async _ => await LoginPlatformAsync());
        LogoutPlatformCommand = new RelayCommand(async _ => await LogoutPlatformAsync());
        PreviousMonthCommand = new RelayCommand(async _ => await GoToPreviousMonthAsync());
        NextMonthCommand = new RelayCommand(async _ => await GoToNextMonthAsync(), _ => CanGoToNextMonth());
        OpenDetailedStatsCommand = new RelayCommand(_ => OpenDetailedStats());

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

    private Window? GetWindowOwner()
    {
        if (Application.Current == null)
        {
            return null;
        }
        var activeDashboard = Application.Current.Windows.OfType<Windows.DashboardWindow>().FirstOrDefault();
        if (activeDashboard != null && activeDashboard.IsLoaded)
        {
            return activeDashboard;
        }
        var mainWin = Application.Current.MainWindow;
        if (mainWin != null && mainWin.IsLoaded)
        {
            return mainWin;
        }
        return null;
    }

    private void OpenDetailedStats()
    {
        var vm = new DetailedStatsViewModel(_usageRepo, _platformClient, _settings);
        var window = new Windows.DetailedStatsWindow(vm)
        {
            Owner = GetWindowOwner()
        };
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

        // Použít predikci z PollingService (již správně spočítaná, bez duplicit)
        if (result.Prediction != null)
        {
            Prediction = result.Prediction.FormattedPrediction;
            AvgDailySpend = result.Prediction.FormattedDailySpend;
            DailySpend = result.Prediction.FormattedDailySpend;
        }
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
        var todayLocal = DateTime.Today;

        // Celková spotřeba za posledních 7 kalendářních dní (včetně dneška)
        var weekStartLocal = todayLocal.AddDays(-6);
        var weekSpend = SumSpendByDay(_history, weekStartLocal, todayLocal);
        WeeklySpend = weekSpend >= 0 ? $"${weekSpend:F2}" : "—";

        // Celková spotřeba za posledních 30 kalendářních dní
        var monthStartLocal = todayLocal.AddDays(-29);
        var monthSpend = SumSpendByDay(_history, monthStartLocal, todayLocal);
        MonthlySpend = monthSpend >= 0 ? $"${monthSpend:F2}" : "—";
    }

    /// <summary>
    /// Spočítá celkovou spotřebu v rozsahu kalendářních dnů (lokální datum) tak,
    /// že agreguje SumPositiveDeltas po jednotlivých dnech. Tím se eliminují
    /// problémy s cross-midnight deltami a kolísáním timestampů.
    /// </summary>
    private static decimal SumSpendByDay(List<BalanceSnapshot> history, DateTime startLocal, DateTime endLocal)
    {
        // Seskupit záznamy podle kalendářního dne (lokální čas)
        var byDay = history
            .Select(h => new { h, localDay = h.Timestamp.ToLocalTime().Date })
            .Where(x => x.localDay >= startLocal && x.localDay <= endLocal)
            .GroupBy(x => x.localDay)
            .ToList();

        decimal total = 0;
        foreach (var dayGroup in byDay)
        {
            //var dayRecs = dayGroup.Select(x => x.h).OrderBy(h => h.Timestamp).ToList();
            var dayRecs = dayGroup.Select(x => x.h).ToList();
            if (dayRecs.Count >= 2)
            {
                var daySpend = SpendCalculator.SumPositiveDeltas(dayRecs);
                total += daySpend;
            }
        }
        return total;
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

    public async Task LoginPlatformAsync()
    {
        var loginWindow = new Windows.LoginWindow
        {
            Owner = GetWindowOwner()
        };
        if (loginWindow.ShowDialog() == true && !string.IsNullOrEmpty(loginWindow.CapturedToken))
        {
            IsLoading = true;
            _platformSelectedMonth = DateTime.Today; // reset to current month on fresh login
            await _settings.SetSessionTokenAsync(loginWindow.CapturedToken);
            await LoadPlatformStatsAsync();
            IsLoading = false;
        }
    }

    public async Task LogoutPlatformAsync()
    {
        IsLoading = true;
        _platformSelectedMonth = DateTime.Today; // reset to current month
        await _settings.SetSessionTokenAsync(null);
        await LoadPlatformStatsAsync();
        IsLoading = false;
    }

    private async Task GoToPreviousMonthAsync()
    {
        IsLoading = true;
        _platformSelectedMonth = _platformSelectedMonth.AddMonths(-1);
        await LoadPlatformStatsAsync();
        IsLoading = false;
    }

    private async Task GoToNextMonthAsync()
    {
        if (CanGoToNextMonth())
        {
            IsLoading = true;
            _platformSelectedMonth = _platformSelectedMonth.AddMonths(1);
            await LoadPlatformStatsAsync();
            IsLoading = false;
        }
    }

    private bool CanGoToNextMonth()
    {
        var today = DateTime.Today;
        return _platformSelectedMonth.Year < today.Year ||
               (_platformSelectedMonth.Year == today.Year && _platformSelectedMonth.Month < today.Month);
    }

    public async Task LoadPlatformStatsAsync()
    {
        var token = await _settings.GetSessionTokenAsync();
        if (string.IsNullOrEmpty(token))
        {
            _platformSelectedMonth = DateTime.Today; // reset to current month
            IsPlatformConnected = false;
            PlatformTotalTokens = "—";
            PlatformCost = "—";
            PlatformCacheRatio = "—";
            PlatformHeading = "—";

            // Reset detailních hodnot
            PlatformProInput = "—";
            PlatformProCache = "—";
            PlatformProOutput = "—";
            PlatformProTotal = "—";
            PlatformProCost = "—";

            PlatformFlashInput = "—";
            PlatformFlashCache = "—";
            PlatformFlashOutput = "—";
            PlatformFlashTotal = "—";
            PlatformFlashCost = "—";

            PlatformTotalInput = "—";
            PlatformTotalCache = "—";
            PlatformTotalOutput = "—";
            PlatformTotalTotal = "—";
            PlatformTotalCost = "—";
            return;
        }

        IsPlatformConnected = true;

        try
        {
            var year = _platformSelectedMonth.Year;
            var month = _platformSelectedMonth.Month;
            PlatformHeading = $"{year}-{month:D2}";

            var amountJson = await _platformClient.GetUsageAmountAsync(token, year, month);
            var costJson = await _platformClient.GetUsageCostAsync(token, year, month);

            // Kontrola chybových kódů od DeepSeek platformy
            if (amountJson is JsonObject amountObj && amountObj.ContainsKey("code") && amountObj["code"]?.GetValue<int>() != 0)
            {
                var msg = amountObj["msg"]?.GetValue<string>() ?? "Unknown API Error";
                throw new Exception($"DeepSeek API error (amount): {msg}");
            }
            if (costJson is JsonObject costObj && costObj.ContainsKey("code") && costObj["code"]?.GetValue<int>() != 0)
            {
                var msg = costObj["msg"]?.GetValue<string>() ?? "Unknown API Error";
                throw new Exception($"DeepSeek API error (cost): {msg}");
            }

            // Vybereme pouze sekci "total" (celkový součet za měsíc) pro zamezení duplicit s denním rozpisem
            var amountTotalNode = GetSafeNode(amountJson, "data", "biz_data", "total")
                               ?? GetSafeNode(amountJson, "data", "total")
                               ?? GetSafeNode(amountJson, "total")
                               ?? amountJson;

            long proCacheHit = 0, proCacheMiss = 0, proResponse = 0;
            long flashCacheHit = 0, flashCacheMiss = 0, flashResponse = 0;

            if (amountTotalNode is JsonArray arr)
            {
                foreach (var item in arr)
                {
                    if (item is JsonObject modelObj)
                    {
                        var modelName = modelObj["model"]?.ToString() ?? "";
                        var usageNode = modelObj["usage"];
                        var (hit, miss, resp) = ParseUsageAmount(usageNode);

                        if (modelName.Contains("flash", StringComparison.OrdinalIgnoreCase))
                        {
                            flashCacheHit += hit;
                            flashCacheMiss += miss;
                            flashResponse += resp;
                        }
                        else
                        {
                            proCacheHit += hit;
                            proCacheMiss += miss;
                            proResponse += resp;
                        }
                    }
                }
            }
            else
            {
                var (hit, miss, resp) = ParseUsageAmount(amountTotalNode);
                proCacheHit = hit;
                proCacheMiss = miss;
                proResponse = resp;
            }

            long totalCacheHit = proCacheHit + flashCacheHit;
            long totalCacheMiss = proCacheMiss + flashCacheMiss;
            long totalResponse = proResponse + flashResponse;
            long totalTokens = totalCacheHit + totalCacheMiss + totalResponse;

            // Získání a rozdělení nákladů podle modelů
            var costTotalNode = GetSafeNode(costJson, "data", "biz_data", "0", "total")
                             ?? GetSafeNode(costJson, "data", "biz_data", "total")
                             ?? GetSafeNode(costJson, "data", "total")
                             ?? GetSafeNode(costJson, "total")
                             ?? costJson;

            decimal proCost = 0;
            decimal flashCost = 0;

            if (costTotalNode is JsonArray costArr)
            {
                foreach (var item in costArr)
                {
                    if (item is JsonObject modelObj)
                    {
                        var modelName = modelObj["model"]?.ToString() ?? "";
                        var usageNode = modelObj["usage"];
                        var costVal = ParseUsageCost(usageNode);

                        if (modelName.Contains("flash", StringComparison.OrdinalIgnoreCase))
                        {
                            flashCost += costVal;
                        }
                        else
                        {
                            proCost += costVal;
                        }
                    }
                }
            }
            else
            {
                var costVal = ParseUsageCost(costTotalNode);
                proCost = costVal;
            }

            decimal totalCost = proCost + flashCost;

            // Uložení detailních textů pro zobrazení v UI tabulce
            PlatformProInput = $"{proCacheMiss:N0}";
            PlatformProCache = $"{proCacheHit:N0}";
            PlatformProOutput = $"{proResponse:N0}";
            long proTotal = proCacheMiss + proCacheHit + proResponse;
            PlatformProTotal = $"{proTotal:N0}";
            PlatformProCost = $"${proCost:F2}";

            PlatformFlashInput = $"{flashCacheMiss:N0}";
            PlatformFlashCache = $"{flashCacheHit:N0}";
            PlatformFlashOutput = $"{flashResponse:N0}";
            long flashTotal = flashCacheMiss + flashCacheHit + flashResponse;
            PlatformFlashTotal = $"{flashTotal:N0}";
            PlatformFlashCost = $"${flashCost:F2}";

            PlatformTotalInput = $"{totalCacheMiss:N0}";
            PlatformTotalCache = $"{totalCacheHit:N0}";
            PlatformTotalOutput = $"{totalResponse:N0}";
            PlatformTotalTotal = $"{totalTokens:N0}";
            PlatformTotalCost = $"${totalCost:F2}";

            // Zachování jednoduchých hodnot pro zpětnou kompatibilitu
            PlatformTotalTokens = totalTokens > 0 ? $"{totalTokens:N0}" : "0";
            PlatformCost = $"${totalCost:F2}";

            var loc = LocalizationService.Instance;
            PlatformCacheRatio = loc.Format("platform_tooltip_tokens_v2", 
                $"{proCacheMiss:N0}", $"{proCacheHit:N0}", $"{proResponse:N0}",
                $"{flashCacheMiss:N0}", $"{flashCacheHit:N0}", $"{flashResponse:N0}",
                $"{totalCacheMiss:N0}", $"{totalCacheHit:N0}", $"{totalResponse:N0}");
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to load platform stats", ex);
            PlatformTotalTokens = "error";
            PlatformCost = "error";
            PlatformCacheRatio = ex.Message;

            PlatformProInput = "error";
            PlatformProCache = "error";
            PlatformProOutput = "error";
            PlatformProTotal = "error";
            PlatformProCost = "error";

            PlatformFlashInput = "error";
            PlatformFlashCache = "error";
            PlatformFlashOutput = "error";
            PlatformFlashTotal = "error";
            PlatformFlashCost = "error";

            PlatformTotalInput = "error";
            PlatformTotalCache = "error";
            PlatformTotalOutput = "error";
            PlatformTotalTotal = "error";
            PlatformTotalCost = "error";
        }
    }

    private static (long CacheHit, long CacheMiss, long Response) ParseUsageAmount(JsonNode? node)
    {
        long cacheHit = 0;
        long cacheMiss = 0;
        long response = 0;

        if (node == null) return (0, 0, 0);

        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("type") && (obj.ContainsKey("amount") || obj.ContainsKey("value") || obj.ContainsKey("count")))
            {
                var type = obj["type"]?.ToString();
                var valStr = (obj["amount"] ?? obj["value"] ?? obj["count"])?.ToString();
                
                if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(valStr) &&
                    double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d))
                {
                    var val = (long)d;
                    if (type.Equals("PROMPT_CACHE_HIT_TOKEN", System.StringComparison.OrdinalIgnoreCase))
                        cacheHit += val;
                    else if (type.Equals("PROMPT_CACHE_MISS_TOKEN", System.StringComparison.OrdinalIgnoreCase))
                        cacheMiss += val;
                    else if (type.Equals("RESPONSE_TOKEN", System.StringComparison.OrdinalIgnoreCase))
                        response += val;
                    else if (type.Equals("PROMPT_TOKEN", System.StringComparison.OrdinalIgnoreCase) && val > 0)
                        cacheMiss += val;
                }
            }

            foreach (var kv in obj)
            {
                var sub = ParseUsageAmount(kv.Value);
                cacheHit += sub.CacheHit;
                cacheMiss += sub.CacheMiss;
                response += sub.Response;
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                var sub = ParseUsageAmount(item);
                cacheHit += sub.CacheHit;
                cacheMiss += sub.CacheMiss;
                response += sub.Response;
            }
        }

        return (cacheHit, cacheMiss, response);
    }

    private static decimal ParseUsageCost(JsonNode? node)
    {
        if (node == null) return 0;
        decimal sum = 0;

        if (node is JsonObject obj)
        {
            if (obj.ContainsKey("type") && (obj.ContainsKey("cost") || obj.ContainsKey("amount") || obj.ContainsKey("value")))
            {
                var valStr = (obj["cost"] ?? obj["amount"] ?? obj["value"])?.ToString();
                var type = obj["type"]?.ToString();
                if (type != null && type.Equals("REQUEST", System.StringComparison.OrdinalIgnoreCase))
                {
                    valStr = null;
                }

                if (!string.IsNullOrEmpty(valStr) &&
                    decimal.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    sum += parsed;
                }
            }

            foreach (var kv in obj)
            {
                sum += ParseUsageCost(kv.Value);
            }
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
            {
                sum += ParseUsageCost(item);
            }
        }

        return sum;
    }

    private static JsonNode? GetSafeNode(JsonNode? node, params string[] path)
    {
        if (node == null) return null;
        var current = node;
        foreach (var key in path)
        {
            if (current is JsonObject obj && obj.ContainsKey(key))
            {
                current = obj[key];
            }
            else if (current is JsonArray arr && int.TryParse(key, out var index) && index >= 0 && index < arr.Count)
            {
                current = arr[index];
            }
            else
            {
                return null;
            }
        }
        return current;
    }
}
