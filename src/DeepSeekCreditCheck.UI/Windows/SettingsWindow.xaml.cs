using System.Windows;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadAsync();
    }
}
