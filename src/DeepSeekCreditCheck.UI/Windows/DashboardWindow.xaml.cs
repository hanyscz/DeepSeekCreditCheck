using System.Windows;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class DashboardWindow : Window
{
    public DashboardWindow(DashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closing += (_, e) =>
        {
            e.Cancel = true;   // Neukončit aplikaci
            Hide();            // Jen schovat okno
        };
    }
}
