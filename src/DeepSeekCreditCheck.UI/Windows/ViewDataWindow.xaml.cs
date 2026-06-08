using System.Collections.ObjectModel;
using System.Windows;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows
{
    public partial class ViewDataWindow : Window
    {
        public ViewDataViewModel ViewModel { get; }

        public ViewDataWindow(IBalanceRepository balanceRepo)
        {
            InitializeComponent();
            ViewModel = new ViewDataViewModel(balanceRepo);
            DataContext = ViewModel;
            Loaded += async (_, _) => await ViewModel.LoadAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

namespace DeepSeekCreditCheck.UI.ViewModels
{
    public class ViewDataViewModel : BaseViewModel
    {
        private readonly IBalanceRepository _balanceRepo;
        private string _recordCount = "0";
        private ObservableCollection<BalanceSnapshot> _records = new();

        public string RecordCount { get => _recordCount; set => SetProperty(ref _recordCount, value); }
        public ObservableCollection<BalanceSnapshot> Records { get => _records; set => SetProperty(ref _records, value); }

        public ViewDataViewModel(IBalanceRepository balanceRepo)
        {
            _balanceRepo = balanceRepo;
        }

        public async Task LoadAsync()
        {
            var all = await _balanceRepo.GetAllAsync(limit: 10000);
            Records = new ObservableCollection<BalanceSnapshot>(
                all.OrderByDescending(r => r.Timestamp));
            RecordCount = Records.Count.ToString();
        }
    }
}
