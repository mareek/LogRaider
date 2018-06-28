using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogRaider
{
    public class ZipService
    {
        public long CompressDirectoryParallel(DirectoryInfo logDirectory)
        {
            long zipedFileSize = 0L;
            var filesByArchive = logDirectory.EnumerateFiles().Where(IsArchiveLogFile).GroupBy(GetArchivePath);
            Parallel.ForEach(filesByArchive,
                             () => 0L,
                             (fileGroup, loupState, subTotal) => subTotal + AddFilesToArchiveAndDelete(new FileInfo(fileGroup.Key), fileGroup.ToList()),
                             finalResult => Interlocked.Add(ref zipedFileSize, finalResult));
            return zipedFileSize;

        }

        private long AddFilesToArchiveAndDelete(FileInfo archiveFile, IList<FileInfo> logFiles)
        {
            long zipedFileSize = 0L;
            if (!archiveFile.Exists)
            {
                CreateArchive(archiveFile);
            }

            using (var zipArchive = ZipFile.Open(archiveFile.FullName, ZipArchiveMode.Update))
            {
                foreach (var logFile in logFiles)
                {
                    if (zipArchive.Entries.All(e => e.FullName != logFile.Name))
                    {
                        zipedFileSize += logFile.Length;
                        zipArchive.CreateEntryFromFile(logFile.FullName, logFile.Name, CompressionLevel.Optimal);
                    }
                }
            }

            foreach (var logFile in logFiles)
            {
                logFile.Delete();
            }

            return zipedFileSize;
        }

        private void CreateArchive(FileInfo archiveFile) => ZipFile.Open(archiveFile.FullName, ZipArchiveMode.Create).Dispose();

        private static string GetArchivePath(FileInfo logFile)
        {
            var weekStart = GetLogFileDate(logFile);
            while (weekStart.DayOfWeek != DayOfWeek.Monday)
            {
                weekStart = weekStart.AddDays(-1);
            }

            var weekEnd = weekStart.AddDays(6);
            var archiveName = $"Debug {weekStart:yyyy MM-dd} to {weekEnd:MM-dd}.zip";
            return Path.Combine(logFile.DirectoryName, archiveName);
        }

        public static bool IsZipFile(FileInfo file) => file.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);

        private static bool IsArchiveLogFile(FileInfo file)
        {
            string extension = file.Extension;
            return extension.StartsWith(".log", StringComparison.OrdinalIgnoreCase) && extension.Length == 14 && char.IsDigit(extension.Last());
        }

        private static DateTime GetLogFileDate(FileInfo file) => DateTime.ParseExact(file.Extension.Substring(4), "yyyy-MM-dd", CultureInfo.CurrentCulture);
    }
}
