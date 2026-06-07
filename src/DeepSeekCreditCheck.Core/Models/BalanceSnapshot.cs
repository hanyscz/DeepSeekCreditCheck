using System.Globalization;

namespace DeepSeekCreditCheck.Core.Models;

public class BalanceSnapshot
{
    public int SnapshotId { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsAvailable { get; set; }
    public string Currency { get; set; } = "USD";
    public string TotalBalance { get; set; } = "0.00";
    public string GrantedBalance { get; set; } = "0.00";
    public string ToppedUpBalance { get; set; } = "0.00";

    public decimal TotalBalanceDecimal => ParseDecimal(TotalBalance);
    public decimal GrantedBalanceDecimal => ParseDecimal(GrantedBalance);
    public decimal ToppedUpBalanceDecimal => ParseDecimal(ToppedUpBalance);

    private static decimal ParseDecimal(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0;
}
