using System;

namespace DeepSeekCreditCheck.Core.Services;

public enum TariffType
{
    OffPeak,
    Peak
}

public class TariffInfo
{
    public bool IsPeak { get; set; }
    public TariffType TariffType { get; set; }
    public DateTime TransitionUtc { get; set; }
    public DateTime TransitionLocal { get; set; }
    public TimeSpan Remaining { get; set; }
    public string FormattedLocalTime { get; set; } = string.Empty;
    public string FormattedRemaining { get; set; } = string.Empty;
}

public static class TariffService
{
    /// <summary>
    /// Určí, zda v daný UTC čas platí špičkový tarif (Peak).
    /// Špičková okna (UTC): 01:00 - 04:00 a 06:00 - 10:00.
    /// </summary>
    public static bool IsPeak(DateTime utcTime)
    {
        var time = utcTime.TimeOfDay;
        var p1Start = new TimeSpan(1, 0, 0);
        var p1End = new TimeSpan(4, 0, 0);
        var p2Start = new TimeSpan(6, 0, 0);
        var p2End = new TimeSpan(10, 0, 0);

        return (time >= p1Start && time < p1End) ||
               (time >= p2Start && time < p2End);
    }

    /// <summary>
    /// Získá detailní informace o aktuálním tarifu, čase konce/začátku špičky a odpočtu.
    /// </summary>
    public static TariffInfo GetTariffInfo(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var date = now.Date;

        var p1Start = date.AddHours(1);
        var p1End = date.AddHours(4);
        var p2Start = date.AddHours(6);
        var p2End = date.AddHours(10);
        var nextP1Start = date.AddDays(1).AddHours(1);

        bool isPeak;
        DateTime transitionUtc;

        if (now < p1Start)
        {
            isPeak = false;
            transitionUtc = p1Start;
        }
        else if (now < p1End)
        {
            isPeak = true;
            transitionUtc = p1End;
        }
        else if (now < p2Start)
        {
            isPeak = false;
            transitionUtc = p2Start;
        }
        else if (now < p2End)
        {
            isPeak = true;
            transitionUtc = p2End;
        }
        else
        {
            isPeak = false;
            transitionUtc = nextP1Start;
        }

        var transitionLocal = transitionUtc.ToLocalTime();
        var remaining = transitionUtc > now ? transitionUtc - now : TimeSpan.Zero;

        int hours = (int)remaining.TotalHours;
        int minutes = remaining.Minutes;

        string remainingStr;
        if (hours > 0)
        {
            remainingStr = $"za {hours} h {minutes} min";
        }
        else
        {
            remainingStr = $"za {minutes} min";
        }

        return new TariffInfo
        {
            IsPeak = isPeak,
            TariffType = isPeak ? TariffType.Peak : TariffType.OffPeak,
            TransitionUtc = transitionUtc,
            TransitionLocal = transitionLocal,
            Remaining = remaining,
            FormattedLocalTime = transitionLocal.ToString("HH:mm"),
            FormattedRemaining = remainingStr
        };
    }
}
