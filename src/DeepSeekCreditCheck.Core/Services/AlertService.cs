namespace DeepSeekCreditCheck.Core.Services;

public class AlertService
{
    private decimal _lastNotifiedBalance = decimal.MaxValue;
    private bool _wasBelowThreshold = false;

    public event EventHandler<AlertEventArgs>? AlertTriggered;

    /// <summary>
    /// Zkontroluje zůstatek proti prahu. Vyvolá AlertTriggered při poklesu pod práh.
    /// </summary>
    public void Check(decimal currentBalance, decimal threshold)
    {
        var isBelow = currentBalance < threshold;

        // Notifikujeme jen při přechodu z "nad prahem" do "pod prahem"
        // nebo když balance dál klesá pod prahem (každých ~10 % poklesu od poslední notifikace)
        if (isBelow)
        {
            if (!_wasBelowThreshold || currentBalance < _lastNotifiedBalance * 0.9m)
            {
                _lastNotifiedBalance = currentBalance;
                AlertTriggered?.Invoke(this, new AlertEventArgs
                {
                    CurrentBalance = currentBalance,
                    Threshold = threshold,
                    Message = $"⚠️ DeepSeek kredit klesl pod ${threshold:F2} — aktuálně ${currentBalance:F2}"
                });
            }
        }
        else
        {
            // Reset — balance je zpět nad prahem
            if (_wasBelowThreshold)
            {
                _lastNotifiedBalance = decimal.MaxValue;
            }
        }

        _wasBelowThreshold = isBelow;
    }
}

public class AlertEventArgs : EventArgs
{
    public decimal CurrentBalance { get; init; }
    public decimal Threshold { get; init; }
    public string Message { get; init; } = string.Empty;
}
