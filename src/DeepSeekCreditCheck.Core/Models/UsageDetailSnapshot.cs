using System;

namespace DeepSeekCreditCheck.Core.Models;

public class UsageDetailSnapshot
{
    public int Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public string UtcDate { get; set; } = "";
    public string Model { get; set; } = "";
    public string ApiKeyName { get; set; } = "";
    public string ApiKeyMasked { get; set; } = "";
    public string Type { get; set; } = "";
    public double? Price { get; set; }
    public long Amount { get; set; }
}
