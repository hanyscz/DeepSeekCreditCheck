using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;

namespace DeepSeekCreditCheck.Core.Services;

public class PollingService : IPollingService
{
    private readonly IDeepSeekApiClient _apiClient;
    private readonly IBalanceRepository _balanceRepo;
    private readonly IAppSettingsService _settings;
    private readonly PredictionEngine _predictionEngine;
    private readonly AlertService _alertService;
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;

    public event EventHandler<PollResult>? PollCompleted;
    public event EventHandler<string>? PollFailed;
    public event EventHandler<RechargeEventArgs>? RechargeDetected;

    /// <summary>Minimální kladná delta zůstatku považovaná za dobití (ochrana proti šumu).</summary>
    private const decimal RechargeMinDelta = 0.01m;

    public PollingService(
        IDeepSeekApiClient apiClient,
        IBalanceRepository balanceRepo,
        IAppSettingsService settings,
        PredictionEngine predictionEngine,
        AlertService alertService)
    {
        _apiClient = apiClient;
        _balanceRepo = balanceRepo;
        _settings = settings;
        _predictionEngine = predictionEngine;
        _alertService = alertService;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var interval = TimeSpan.FromMinutes(await _settings.GetPollingIntervalMinutesAsync());

        await PollOnceAsync(ct);

        _timer = new PeriodicTimer(interval);
        _ = Task.Run(async () =>
        {
            try
            {
                while (await _timer.WaitForNextTickAsync(_cts.Token))
                {
                    await PollOnceAsync(_cts.Token);
                }
            }
            catch (OperationCanceledException) { }
        }, _cts.Token);
    }

    public async Task StopAsync()
    {
        _timer?.Dispose();
        _timer = null;
        _cts?.Cancel();
        await Task.CompletedTask;
    }

    public async Task PollOnceAsync(CancellationToken ct)
    {
        var apiKey = await _settings.GetApiKeyAsync();
        if (string.IsNullOrEmpty(apiKey)) return;

        try
        {
            // Předchozí snapshot v USD pro detekci dobití (musí se načíst před uložením nového)
            var previous = await _balanceRepo.GetLatestAsync("USD");

            // Načíst všechny měny z API (USD, CNY, ...)
            IReadOnlyList<BalanceSnapshot> allBalances;
            try
            {
                allBalances = await _apiClient.GetAllBalancesAsync(apiKey);
            }
            catch
            {
                var single = await _apiClient.GetBalanceAsync(apiKey);
                allBalances = new[] { single };
            }

            if (allBalances == null || allBalances.Count == 0)
            {
                var single = await _apiClient.GetBalanceAsync(apiKey);
                allBalances = new[] { single };
            }

            // Do DB zalogujeme všechny vrácené měny
            foreach (var b in allBalances)
            {
                await _balanceRepo.SaveAsync(b);
            }

            // Pro výpočty a zobrazení bereme výhradně USD
            var snapshot = allBalances.FirstOrDefault(b => string.Equals(b.Currency, "USD", StringComparison.OrdinalIgnoreCase))
                           ?? allBalances.FirstOrDefault()
                           ?? new BalanceSnapshot { Currency = "USD", TotalBalance = "0.00", Timestamp = DateTime.UtcNow };

            // Detekce dobití kreditu — kladný skok zůstatku v USD oproti předchozímu záznamu v USD
            if (previous != null &&
                string.Equals(previous.Currency, "USD", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(snapshot.Currency, "USD", StringComparison.OrdinalIgnoreCase))
            {
                var delta = snapshot.TotalBalanceDecimal - previous.TotalBalanceDecimal;
                if (delta > RechargeMinDelta)
                {
                    RechargeDetected?.Invoke(this, new RechargeEventArgs
                    {
                        Amount = delta,
                        NewBalance = snapshot.TotalBalanceDecimal
                    });
                }
            }

            var history = await _balanceRepo.GetAllAsync(limit: 500, currency: "USD");
            var prediction = _predictionEngine.Calculate(history, snapshot.TotalBalanceDecimal);

            // Spočítat dnešní spotřebu z historie (výhradně USD)
            // Používáme lokální kalendářní den pro správné filtrování bez ohledu na DateTime.Kind
            decimal? todaySpend = null;
            var todayLocal = DateTime.Today;
            var todayRecs = history
                .Where(h => h.Timestamp.ToLocalTime().Date == todayLocal)
                .OrderBy(h => h.Timestamp)
                .ToList();
            if (todayRecs.Count >= 2)
            {
                // SumPositiveDeltas ignoruje dobíjení (stejná metoda jako u historických dnů)
                var spend = SpendCalculator.SumPositiveDeltas(todayRecs);
                if (spend > 0)
                    todaySpend = spend;
            }

            var thresholdStr = await _settings.GetAlertThresholdAsync();
            var threshold = decimal.TryParse(thresholdStr, out var t) ? t : 2.00m;
            _alertService.Check(snapshot.TotalBalanceDecimal, threshold);

            PollCompleted?.Invoke(this, new PollResult
            {
                Snapshot = snapshot,
                AllBalances = allBalances,
                Prediction = prediction,
                TodaySpend = todaySpend,
                Timestamp = DateTime.UtcNow,
                Threshold = threshold
            });
        }
        catch (HttpRequestException ex)
        {
            Logger.Error($"API nedostupné (StatusCode={ex.StatusCode ?? 0})", ex);
            PollFailed?.Invoke(this, $"API nedostupné ({ex.StatusCode ?? 0})");
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.Error("Poll selhal", ex);
            PollFailed?.Invoke(this, $"Chyba: {ex.Message}");
        }
    }
}

public class PollResult
{
    public Models.BalanceSnapshot? Snapshot { get; init; }
    public IReadOnlyList<Models.BalanceSnapshot> AllBalances { get; init; } = Array.Empty<Models.BalanceSnapshot>();
    public PredictionResult? Prediction { get; init; }
    public decimal? TodaySpend { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal Threshold { get; init; }
}

public class RechargeEventArgs : EventArgs
{
    /// <summary>Výše dobití (kladná delta zůstatku).</summary>
    public decimal Amount { get; init; }

    /// <summary>Zůstatek po dobití.</summary>
    public decimal NewBalance { get; init; }
}
