namespace DeepSeekCreditCheck.Core.Services;

public static class Logger
{
    private static string? _logPath;
    private static readonly object _lock = new();

    public static void Init(string logPath)
    {
        _logPath = logPath;
    }

    public static void Info(string message)
    {
        WriteLine("INFO", message);
    }

    public static void Warn(string message)
    {
        WriteLine("WARN", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var text = ex != null ? $"{message} ({ex.GetType().Name}: {ex.Message})" : message;
        WriteLine("ERROR", text);
    }

    private static void WriteLine(string level, string message)
    {
        if (_logPath == null) return;

        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {level}  {message}";
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line + Environment.NewLine);
            }
            catch
            {
                // Logování nesmí nikdy spadnout
            }
        }
    }
}
