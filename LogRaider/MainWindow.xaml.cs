using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
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
        private const int Mega = 1024 * 1024;

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

        private async void btnLaunch_Click(object sender, RoutedEventArgs e) => await ExecuteLongAction(() => ShowAnalysisResult(GetAnalysis(), SelectedLogDirectory));

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
            if (LogDirectory.IsLogDirectory(SelectedDirectory))
            {
                await DownloadAndCompressDirectory(SelectedDirectory);
            }

            foreach (var directory in SelectedDirectory.EnumerateDirectories("*", SearchOption.AllDirectories).Where(LogDirectory.IsLogDirectory))
            {
                await DownloadAndCompressDirectory(directory);
                WriteLineToConsole();
            }
        }

        private async Task DownloadAndCompressDirectory(DirectoryInfo directory)
        {
            var logDirectory = new LogDirectory(directory);
            await DownloadDirectory(logDirectory);
            await CompressDirectory(directory);
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
            else
            {
                throw new ArgumentOutOfRangeException("aucune analyse sélectionnée");
            }
        }

        private async Task ShowAnalysisResult(ILogAnalysis analysis, LogDirectory logDirectory)
        {
            await ExecuteTimedLongTask($"Début [{analysis.Name}] dans {logDirectory.Name}...",
                                       "Temps de calcul",
                                       () => AnalyseDirectory(analysis));
            WriteLineToConsole($"Taille totale des logs : {logDirectory.GetSize() / Mega} MB");
        }

        private async Task DownloadDirectory(LogDirectory logDirectory)
        {
            var downloadSize = await ExecuteTimedLongTask($"Début du téléchargement de {logDirectory.Name} ...",
                                                          "Temps de téléchargement",
                                                          logDirectory.Download);
            WriteLineToConsole($"Taille des fichiers téléchargés : {downloadSize / Mega} MB");
        }

        private async Task CompressDirectory(DirectoryInfo directory)
        {
            var compressedSize = await ExecuteTimedLongTask($"Début de la compression de {directory.Name} ...",
                                                            "Temps de compression",
                                                            () => new ZipService().CompressDirectoryParallel(directory));

            WriteLineToConsole($"Taille des fichiers compressés : {compressedSize / Mega} MB");

            // The Compression process can allocate huge chunks of memory so we release it after
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        private async Task ExecuteTimedLongTask(string startMessage, string endMessage, Action longTask)
            => await ExecuteTimedLongTaskInternal(startMessage, endMessage, () => Task.Run(() => { longTask(); return 0; }));

        private async Task<T> ExecuteTimedLongTask<T>(string startMessage, string endMessage, Func<T> longTask)
            => await ExecuteTimedLongTaskInternal(startMessage, endMessage, () => Task.Run(longTask));

        private async Task<T> ExecuteTimedLongTaskInternal<T>(string startMessage, string endMessage, Func<Task<T>> longTask)
        {
            WriteLineToConsole(startMessage);
            var chrono = Stopwatch.StartNew();
            var result = await longTask();
            chrono.Stop();
            WriteLineToConsole($"{endMessage} : {chrono.Elapsed}");
            return result;
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
    }
}
