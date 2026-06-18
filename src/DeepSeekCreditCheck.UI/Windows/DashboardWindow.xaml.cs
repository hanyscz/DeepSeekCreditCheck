using System.Windows;
using DeepSeekCreditCheck.UI.Services;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        Icon = WindowIconHelper.GetIcon();
        DataContext = viewModel;
        Closing += (_, e) =>
        {
            e.Cancel = true;
            Hide();
        };
        IsVisibleChanged += async (s, e) =>
        {
            if ((bool)e.NewValue)
            {
                if (DataContext is DashboardViewModel vm)
                {
                    await vm.LoadPlatformStatsAsync();
                }
            }
        };
    }
}
