using Moq;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class PollingServiceRechargeTests
{
    private static (PollingService svc, Mock<IBalanceRepository> repo, Mock<IDeepSeekApiClient> api)
        CreateService(BalanceSnapshot? previous, string newBalance)
    {
        var api = new Mock<IDeepSeekApiClient>();
        api.Setup(a => a.GetBalanceAsync(It.IsAny<string>()))
            .ReturnsAsync(new BalanceSnapshot
            {
                Timestamp = DateTime.UtcNow,
                Currency = "USD",
                TotalBalance = newBalance
            });

        var repo = new Mock<IBalanceRepository>();
        repo.Setup(r => r.GetLatestAsync()).ReturnsAsync(previous);
        repo.Setup(r => r.SaveAsync(It.IsAny<BalanceSnapshot>())).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetAllAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<BalanceSnapshot>());

        var settings = new Mock<IAppSettingsService>();
        settings.Setup(s => s.GetApiKeyAsync()).ReturnsAsync("test-key");
        settings.Setup(s => s.GetAlertThresholdAsync()).ReturnsAsync("2.00");
        settings.Setup(s => s.GetPollingIntervalMinutesAsync()).ReturnsAsync(15);

        var svc = new PollingService(
            api.Object, repo.Object, settings.Object,
            new PredictionEngine(), new AlertService());

        return (svc, repo, api);
    }

    private static BalanceSnapshot Snapshot(string balance, int minutesAgo = 15) => new()
    {
        Timestamp = DateTime.UtcNow.AddMinutes(-minutesAgo),
        Currency = "USD",
        TotalBalance = balance
    };

    [Fact]
    public async Task PollOnce_BalanceIncreased_FiresRechargeDetected()
    {
        var (svc, _, _) = CreateService(Snapshot("5.00"), "15.00");
        RechargeEventArgs? recharge = null;
        svc.RechargeDetected += (_, args) => recharge = args;

        await svc.PollOnceAsync(CancellationToken.None);

        Assert.NotNull(recharge);
        Assert.Equal(10.00m, recharge!.Amount);
        Assert.Equal(15.00m, recharge.NewBalance);
    }

    [Fact]
    public async Task PollOnce_BalanceDecreased_NoRechargeEvent()
    {
        var (svc, _, _) = CreateService(Snapshot("5.00"), "4.50");
        var fired = false;
        svc.RechargeDetected += (_, _) => fired = true;

        await svc.PollOnceAsync(CancellationToken.None);

        Assert.False(fired);
    }

    [Fact]
    public async Task PollOnce_FirstPollNoHistory_NoRechargeEvent()
    {
        var (svc, _, _) = CreateService(previous: null, "15.00");
        var fired = false;
        svc.RechargeDetected += (_, _) => fired = true;

        await svc.PollOnceAsync(CancellationToken.None);

        Assert.False(fired);
    }

    [Fact]
    public async Task PollOnce_TinyPositiveDelta_NoRechargeEvent()
    {
        // Delta 0.01 je na hranici šumu — nesmí spustit notifikaci
        var (svc, _, _) = CreateService(Snapshot("5.00"), "5.01");
        var fired = false;
        svc.RechargeDetected += (_, _) => fired = true;

        await svc.PollOnceAsync(CancellationToken.None);

        Assert.False(fired);
    }

    [Fact]
    public async Task PollOnce_PollResultContainsThreshold()
    {
        var (svc, _, _) = CreateService(Snapshot("5.00"), "4.00");
        PollResult? result = null;
        svc.PollCompleted += (_, r) => result = r;

        await svc.PollOnceAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(2.00m, result!.Threshold);
    }
}
