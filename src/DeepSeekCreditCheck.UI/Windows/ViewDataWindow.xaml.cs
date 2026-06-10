using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using DeepSeekCreditCheck.Core.Models;
using DeepSeekCreditCheck.Core.Repositories;
using DeepSeekCreditCheck.UI.Services;
using DeepSeekCreditCheck.UI.ViewModels;

namespace DeepSeekCreditCheck.UI.Windows
{
    public partial class ViewDataWindow : Window
    {
        private readonly IBalanceRepository _balanceRepo;
        public ViewDataViewModel ViewModel { get; }

        public ViewDataWindow(IBalanceRepository balanceRepo)
        {
            InitializeComponent();
            Icon = WindowIconHelper.GetIcon();
            _balanceRepo = balanceRepo;
            ViewModel = new ViewDataViewModel(balanceRepo);
            DataContext = ViewModel;
            Loaded += async (_, _) => await ViewModel.LoadAsync();
        }

        private async void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            var allRecords = ViewModel.Records.ToList();
            if (allRecords.Count == 0)
            {
                MessageBox.Show(this, "Žádné záznamy k exportu.", "Prázdná data",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = $"DeepSeek_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
                DefaultExt = ".csv",
                Filter = "CSV soubory (*.csv)|*.csv|Textové soubory (*.txt)|*.txt|Všechny soubory (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    // Hlavička
                    sb.AppendLine("Datum;Čas;Měna;Zůstatek (USD)");
                    // Data — seřazeno od nejnovějších (stejně jako v tabulce)
                    foreach (var r in allRecords.OrderByDescending(r => r.Timestamp))
                    {
                        var localTime = r.Timestamp.ToLocalTime();
                        sb.AppendLine($"{localTime:dd.MM.yyyy};{localTime:HH:mm};{r.Currency};{r.TotalBalanceDecimal:F2}");
                    }

                    await File.WriteAllTextAsync(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show(this,
                        $"Exportováno {allRecords.Count} záznamů do:\n{dialog.FileName}",
                        "Export dokončen",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this,
                        $"Chyba při exportu: {ex.Message}",
                        "Chyba exportu",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            var selected = RecordsGrid.SelectedItems.Cast<BalanceSnapshot>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(this, "Nejprve vyber záznamy k smazání.", "Nic nevybráno",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(this,
                $"Opravdu smazat {selected.Count} vybraných záznamů?\nTuto akci nelze vrátit.",
                "Potvrzení smazání",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _balanceRepo.DeleteAsync(selected.Select(s => s.SnapshotId));
                await ViewModel.LoadAsync();
            }
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
