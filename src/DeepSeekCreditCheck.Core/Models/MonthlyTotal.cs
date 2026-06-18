using System;

namespace DeepSeekCreditCheck.Core.Models;

public class MonthlyTotal
{
    public int Year { get; set; }
    public int Month { get; set; }
    public double TotalCost { get; set; }
    public long TotalTokens { get; set; }
    public long RequestCount { get; set; }
    public long CacheHitTokens { get; set; }
    public long CacheMissTokens { get; set; }
    public long OutputTokens { get; set; }
}
