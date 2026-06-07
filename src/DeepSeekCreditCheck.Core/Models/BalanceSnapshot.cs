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

    // Helper properta pro výpočty
    public decimal TotalBalanceDecimal => decimal.TryParse(TotalBalance, out var v) ? v : 0;
    public decimal GrantedBalanceDecimal => decimal.TryParse(GrantedBalance, out var v) ? v : 0;
    public decimal ToppedUpBalanceDecimal => decimal.TryParse(ToppedUpBalance, out var v) ? v : 0;
}
