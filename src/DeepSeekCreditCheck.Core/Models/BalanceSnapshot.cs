using System.Globalization;

namespace DeepSeekCreditCheck.Core.Models;

public class BalanceSnapshot
{
    private DateTime _timestamp;

    public int SnapshotId { get; set; }

    public DateTime Timestamp
    {
        get => _timestamp;
        set => _timestamp = value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc)
            : value;
    }

    public string Currency { get; set; } = "USD";
    public string TotalBalance { get; set; } = "0.00";

    public decimal TotalBalanceDecimal =>
        decimal.TryParse(TotalBalance, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
