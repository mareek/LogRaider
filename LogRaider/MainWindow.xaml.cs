using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            txtFolder.Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        }

        private async void btnSelectFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new CommonOpenFileDialog())
            {
                dialog.InitialDirectory = txtFolder.Text;
                dialog.IsFolderPicker = true;
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    txtFolder.Text = dialog.FileName;
                    await ExecuteLongAction(() => ShowAnalysisResult(new DirectoryInfo(txtFolder.Text), new MemoryAnalysis()));
                }
            }
        }

        private async Task ShowAnalysisResult(DirectoryInfo directory, ILogAnalysis analysis)
        {
            WriteLineToConsole("Début de la compression ...");
            var chronoCompression = Stopwatch.StartNew();
            var zipService = new ZipService();
            await Task.Run(() => zipService.CompressDirectoryParallel(directory));
            chronoCompression.Stop();
            WriteLineToConsole($"Temps de compression : {chronoCompression.Elapsed}");

            WriteLineToConsole($"Début [{analysis.Name}] ...");

            var chronoTraitement = Stopwatch.StartNew();
            var logDirectory = new LogDirectory(directory);
            txtOutput.Text = await Task.Run(() => analysis.AnalyseLogs(logDirectory.ReadParallel(analysis.Filter)));
            chronoTraitement.Stop();
            WriteLineToConsole($"Temps de calcul : {chronoTraitement.Elapsed}");
            WriteLineToConsole($"Taille totale des logs : {logDirectory.GetSize() / (1024 * 1024)} MB");
        }

        private void WriteLineToConsole(string line)
        {
            txtConsole.Text = txtConsole.Text + line + "\r\n";
            txtConsole.ScrollToEnd();
        }

        private async Task ExecuteLongAction(Func<Task> longAction)
        {
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                await longAction();
            }
            finally
            {
                Mouse.OverrideCursor = null;
                IsEnabled = true;
            }
        }

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
