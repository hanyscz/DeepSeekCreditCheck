namespace DeepSeekCreditCheck.Core.Models;

public class UsageRecord
{
    public int RecordId { get; set; }
    public DateTime Timestamp { get; set; }
    public DateTime? PeriodStart { get; set; }
    public DateTime? PeriodEnd { get; set; }
    public long TotalTokens { get; set; }
    public long InputTokens { get; set; }
    public long OutputTokens { get; set; }
    public long? CachedTokens { get; set; }
}
