using System.Windows;
using System.Windows.Media.Animation;
using DeepSeekCreditCheck.UI.Services;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class NotificationToast : Window
{
    private readonly System.Timers.Timer _closeTimer;
    private readonly Action? _action;

    public NotificationToast(string message, string? actionText = null, Action? action = null)
    {
        InitializeComponent();
        Icon = WindowIconHelper.GetIcon();
        MessageText.Text = message;

        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Bottom - Height - 16;

        if (actionText != null && action != null)
        {
            _action = action;
            ActionButton.Content = actionText;
            ActionButton.Visibility = Visibility.Visible;

            // Pokud je tlačítko, nenechávat automaticky zavřít — uživatel si musí vybrat
            _closeTimer = new System.Timers.Timer(8000) { AutoReset = false };
        }
        else
        {
            _closeTimer = new System.Timers.Timer(4000) { AutoReset = false };
        }

        _closeTimer.Elapsed += (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(400));
                fadeOut.Completed += (_, _) => Close();
                BeginAnimation(OpacityProperty, fadeOut);
            });
        };
        _closeTimer.Start();

        MouseDown += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        _closeTimer.Stop();
        Close();
        _action?.Invoke();
    }

    public static void Show(string message)
    {
        var toast = new NotificationToast(message);
        toast.Show();
    }

    public static void Show(string message, string actionText, Action action)
    {
        var toast = new NotificationToast(message, actionText, action);
        toast.Show();
    }
}
