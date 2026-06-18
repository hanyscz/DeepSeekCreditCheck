using System.Windows;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class DetailedStatsWindow : Window
{
    public DetailedStatsWindow(DetailedStatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
