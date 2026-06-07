namespace DeepSeekCreditCheck.Core.Services;

public interface IPollingService
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
    event EventHandler<PollResult>? PollCompleted;
}
