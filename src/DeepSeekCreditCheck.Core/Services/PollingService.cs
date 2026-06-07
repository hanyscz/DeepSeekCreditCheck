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
            var snapshot = await _apiClient.GetBalanceAsync(apiKey);
            await _balanceRepo.SaveAsync(snapshot);

            var history = await _balanceRepo.GetAllAsync(limit: 500);
            var prediction = _predictionEngine.Calculate(history, snapshot.TotalBalanceDecimal);

            var thresholdStr = await _settings.GetAlertThresholdAsync();
            var threshold = decimal.TryParse(thresholdStr, out var t) ? t : 2.00m;
            _alertService.Check(snapshot.TotalBalanceDecimal, threshold);

            PollCompleted?.Invoke(this, new PollResult
            {
                Snapshot = snapshot,
                Prediction = prediction,
                Timestamp = DateTime.UtcNow
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
    public PredictionResult? Prediction { get; init; }
    public DateTime Timestamp { get; init; }
}
