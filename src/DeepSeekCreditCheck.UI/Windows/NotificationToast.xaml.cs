using System.Windows;
using System.Windows.Media.Animation;
using DeepSeekCreditCheck.UI.Services;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class NotificationToast : Window
{
    private readonly System.Timers.Timer _closeTimer;

    public NotificationToast(string message)
    {
        InitializeComponent();
        Icon = WindowIconHelper.GetIcon();
        MessageText.Text = message;

        Left = SystemParameters.WorkArea.Right - Width - 16;
        Top = SystemParameters.WorkArea.Bottom - Height - 16;

        _closeTimer = new System.Timers.Timer(4000) { AutoReset = false };
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

    public static void Show(string message)
    {
        var toast = new NotificationToast(message);
        toast.Show();
    }
}
