using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LogRaider.Analysis;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace LogRaider
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private LogDirectory SelectedLogDirectory => new LogDirectory(SelectedDirectory);

        private DirectoryInfo SelectedDirectory => new DirectoryInfo(GetSelectedDirectoryPath());

        private string GetSelectedDirectoryPath() => Dispatcher.Invoke(() => txtFolder.Text);

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.InitialDirectory = txtFolder.Text;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtFolder.Text = dialog.FileName;
                    btnLaunch.Focus();
                }
            }
        }

        private async void btnLaunch_Click(object sender, RoutedEventArgs e) => await ExecuteLongAction(ShowAnalysisResult);

        private async void btnDownload_Click(object sender, RoutedEventArgs e) => await ExecuteLongAction(DownloadAndCompressDirectory);

        private async Task ExecuteLongAction(Func<Task> longAction)
        {
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                SetOutput("");
                WriteLineToConsole();
                await longAction();
            }
            finally
            {
                Mouse.OverrideCursor = null;
                IsEnabled = true;
            }
        }

        private async Task DownloadAndCompressDirectory()
        {
            await DownloadDirectory();
            await CompressDirectory();
        }

        private ILogAnalysis GetAnalysis()
        {
            if (radioMemoryAnalysis.IsChecked.GetValueOrDefault())
            {
                return new MemoryAnalysis();
            }
            else if (radioSearchAnalysis.IsChecked.GetValueOrDefault())
            {
                return new SimpleTextSearchAnalysis(txtSearch.Text, chkFullMessageSearch.IsChecked.GetValueOrDefault());
            }
            else if (radioCalculArenhAnalysis.IsChecked.GetValueOrDefault())
            {
                return new CalculArenhAnalysis();
            }
            else
            {
                throw new ArgumentOutOfRangeException("aucune analyse sélectionnée");
            }
        }

        private async Task ShowAnalysisResult()
        {
            var analysis = GetAnalysis();
            await ExecuteTimedLongTask($"Début [{analysis.Name}] dans {SelectedDirectory.Name}...",
                                       "Temps de calcul",
                                       () => AnalyseDirectory(analysis));
            WriteLineToConsole($"Taille totale des logs : {SelectedLogDirectory.GetSize() / (1024 * 1024)} MB");
        }

        private async Task DownloadDirectory() => await ExecuteTimedLongTask($"Début du téléchargement de {SelectedDirectory.Name} ...",
                                                                             "Temps de téléchargement",
                                                                             SelectedLogDirectory.Download);

        private async Task CompressDirectory() => await ExecuteTimedLongTask($"Début de la compression de {SelectedDirectory.Name} ...",
                                                                             "Temps de compression",
                                                                             () => new ZipService().CompressDirectoryParallel(SelectedDirectory));

        private async Task ExecuteTimedLongTask(string startMessage, string endMessage, Action longTask)
            => await ExecuteTimedLongTask(startMessage, endMessage, () => Task.Run(longTask));

        private async Task ExecuteTimedLongTask(string startMessage, string endMessage, Func<Task> longTask)
        {
            WriteLineToConsole(startMessage);
            var chrono = Stopwatch.StartNew();
            await longTask();
            chrono.Stop();
            WriteLineToConsole($"{endMessage} : {chrono.Elapsed}");
        }

        private void AnalyseDirectory(ILogAnalysis analysis)
        {
            IEnumerable<LogEntry> logEntries;
            if (analysis.CanBeParalelyzed)
            {
                logEntries = SelectedLogDirectory.ReadParallel(analysis.Filter);
            }
            else
            {
                logEntries = SelectedLogDirectory.ReadSequential(analysis.Filter);
            }

            SetOutput(analysis.AnalyseLogs(logEntries));
        }

        private void WriteLineToConsole(string line = "")
        {
            Dispatcher.Invoke(() =>
            {
                txtConsole.Text = txtConsole.Text + line + "\r\n";
                txtConsole.ScrollToEnd();
            });
        }

        private void SetOutput(string text) => Dispatcher.Invoke(() => txtOutput.Text = text);

        private string ShowCacheAnalysis(IEnumerable<LogEntry> logEntries) => CountResult.FormatResult(new PricingCacheAnalysis().GetCacheUsageDurations(logEntries).ToList());

        private string ShowAppelsWebService(IEnumerable<LogEntry> logEntries) => CountResult.FormatResult(GetAppelsWebService(logEntries).ToList());

        private string ShowAppelsWebServiceByHour(IEnumerable<LogEntry> logEntries) => CountResult.FormatResult(GetAppelsWebServiceByHour(logEntries).ToList());

        private IEnumerable<CountResult> GetLogLineByHour(IEnumerable<LogEntry> logEntries, Func<LogEntry, bool> filter)
        {
            var countByHeure = logEntries.Where(filter).ToLookup(logEntry => logEntry.DateTime.Hour);
            return Enumerable.Range(0, 24).Select(h => new CountResult(h.ToString("00"), countByHeure[h].Count()));
        }

        private IEnumerable<CountResult> GetAppelsWebService(IEnumerable<LogEntry> logEntries)
        {
            return from logEntry in logEntries
                   where logEntry.Message.StartsWith("Appel ", StringComparison.OrdinalIgnoreCase)
                   group logEntry by logEntry.Message.Substring(6).Split('(').First() into messageGroup
                   select CountResult.FromGrouping(messageGroup);
        }

        private IEnumerable<CountResult> GetAppelsWebServiceByHour(IEnumerable<LogEntry> logEntries) => GetLogLineByHour(logEntries, l => l.Message.StartsWith("Appel "));
    }
}
