namespace DeepSeekCreditCheck.Core.Services;

/// <summary>
/// Stav zůstatku pro barevné rozlišení tray ikony.
/// </summary>
public enum BalanceStatus
{
    /// <summary>Zůstatek neznámý (chyba, chybí API klíč).</summary>
    Unknown,
    /// <summary>Zůstatek v pořádku (&gt; 2× práh).</summary>
    Ok,
    /// <summary>Zůstatek se blíží prahu (≤ 2× práh).</summary>
    Warning,
    /// <summary>Zůstatek pod prahem.</summary>
    Critical
}

/// <summary>
/// Čistá logika pro tray ikonu se zůstatkem — text a stav (barvu) odděleně od GDI vykreslování,
/// aby šla jednotkově testovat.
/// </summary>
public static class TrayIconFormatter
{
    /// <summary>
    /// Určí stav zůstatku vůči prahu.
    /// </summary>
    public static BalanceStatus GetStatus(decimal? balance, decimal threshold)
    {
        if (balance is null) return BalanceStatus.Unknown;
        if (balance < threshold) return BalanceStatus.Critical;
        if (balance <= threshold * 2) return BalanceStatus.Warning;
        return BalanceStatus.Ok;
    }

    /// <summary>
    /// Krátký text zůstatku do ikony: "1.5" pod $10, "42" do $99, "99+" nad, "?" neznámý.
    /// </summary>
    public static string GetIconText(decimal? balance)
    {
        if (balance is null) return "?";
        var b = balance.Value;
        if (b < 0) b = 0;
        if (b < 10) return b.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        if (b < 100) return Math.Floor(b).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        return "99+";
    }
}
