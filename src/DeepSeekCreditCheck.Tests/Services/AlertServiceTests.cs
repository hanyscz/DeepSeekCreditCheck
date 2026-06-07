using System.Globalization;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.Tests.Services;

public class AlertServiceTests
{
    public AlertServiceTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }
    [Fact]
    public void Check_BelowThreshold_TriggersAlert()
    {
        var service = new AlertService();
        AlertEventArgs? args = null;
        service.AlertTriggered += (_, e) => args = e;

        service.Check(1.50m, 2.00m);

        Assert.NotNull(args);
        Assert.Equal(1.50m, args!.CurrentBalance);
        Assert.Equal(2.00m, args.Threshold);
        Assert.Contains("$1.50", args.Message);
    }

    [Fact]
    public void Check_AboveThreshold_DoesNotTrigger()
    {
        var service = new AlertService();
        var triggered = false;
        service.AlertTriggered += (_, _) => triggered = true;

        service.Check(5.00m, 2.00m);

        Assert.False(triggered);
    }

    [Fact]
    public void Check_RepeatedlyBelowThreshold_TriggersOnlyOnSignificantDrop()
    {
        var service = new AlertService();
        var triggerCount = 0;
        service.AlertTriggered += (_, _) => triggerCount++;

        service.Check(1.50m, 2.00m); // 1st — triggers
        service.Check(1.40m, 2.00m); // < 10% drop — should not trigger
        service.Check(1.20m, 2.00m); // > 10% drop from 1.50 — triggers

        Assert.Equal(2, triggerCount);
    }

    [Fact]
    public void Check_BackAboveThreshold_ResetsState()
    {
        var service = new AlertService();
        var triggerCount = 0;
        service.AlertTriggered += (_, _) => triggerCount++;

        service.Check(1.50m, 2.00m); // triggers
        service.Check(3.00m, 2.00m); // back above
        service.Check(1.50m, 2.00m); // drops again — should trigger again

        Assert.Equal(2, triggerCount);
    }
}
