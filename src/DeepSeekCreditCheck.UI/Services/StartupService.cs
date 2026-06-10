using Microsoft.Win32;
using DeepSeekCreditCheck.Core.Services;

namespace DeepSeekCreditCheck.UI.Services;

public interface IStartupService
{
    /// <summary>Zjistí, zda je aplikace registrována pro spuštění při startu Windows.</summary>
    bool IsEnabled();

    /// <summary>Zapne/vypne spuštění při startu Windows. Vrací true při úspěchu.</summary>
    bool SetEnabled(bool enabled);
}

/// <summary>
/// Autostart přes registry HKCU\Software\Microsoft\Windows\CurrentVersion\Run.
/// Registr je jediný zdroj pravdy — stav se nečte z DB.
/// </summary>
public class StartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DeepSeekCreditCheck";

    public bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            return key?.GetValue(ValueName) is string path
                   && string.Equals(path.Trim('"'), GetExePath(), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Logger.Error("Autostart: čtení registru selhalo", ex);
            return false;
        }
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);
            if (enabled)
                key.SetValue(ValueName, $"\"{GetExePath()}\"");
            else if (key.GetValue(ValueName) != null)
                key.DeleteValue(ValueName);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"Autostart: zápis do registru selhal (enabled={enabled})", ex);
            return false;
        }
    }

    private static string GetExePath()
        => Environment.ProcessPath ?? throw new InvalidOperationException("Nelze zjistit cestu k exe");
}
