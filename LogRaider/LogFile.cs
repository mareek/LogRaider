using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace LogRaider
{
    public class LogFile
    {
        private static readonly Encoding DefaultEncoding = Encoding.GetEncoding("ISO-8859-1");

        private readonly FileInfo _file;

        public LogFile(FileInfo file) => _file = file;

        public static bool IsLogFile(FileInfo file) => IsLogFile(file.Name);

        private static bool IsLogFile(string fileName) => fileName.Split('.').LastOrDefault()?.StartsWith("log", StringComparison.OrdinalIgnoreCase) ?? false;

        private bool IsZipFile() => ZipService.IsZipFile(_file);

        public IEnumerable<LogEntry> Read(Func<LogEntry, bool> filter) => Read().Where(filter);

        private IEnumerable<LogEntry> Read() => IsZipFile() ? ReadZipLogFile() : ReadNonZipLogFile();

        private IEnumerable<LogEntry> ReadNonZipLogFile() => ReadLogStream(_file.OpenRead());

        private IEnumerable<LogEntry> ReadZipLogFile()
        {
            using (var zipArchive = ZipFile.OpenRead(_file.FullName))
            {
                foreach (var logEntry in GetLogEntries(zipArchive).SelectMany(e => ReadLogStream(e.Open())))
                {
                    yield return logEntry;
                }
            }
        }

        public long GetLogFileSize() => IsZipFile() ? GetUncompressedZipFileSize() : _file.Length;

        private long GetUncompressedZipFileSize()
        {
            using (var zipArchive = ZipFile.OpenRead(_file.FullName))
            {
                return GetLogEntries(zipArchive).Sum(e => e.Length);
            }
        }

        private static IEnumerable<ZipArchiveEntry> GetLogEntries(ZipArchive zipArchive) => zipArchive.Entries.Where(e => IsLogFile(e.Name));

        private static IEnumerable<LogEntry> ReadLogStream(Stream stream)
        {
            using (var streamReader = new StreamReader(stream, DefaultEncoding))
            {
                if (streamReader.EndOfStream)
                {
                    yield break;
                }

                var currentLogLine = new LogEntry(streamReader.ReadLine());
                while (!streamReader.EndOfStream)
                {
                    var currentLine = streamReader.ReadLine();
                    if (LogEntry.IsLogLine(currentLine))
                    {
                        yield return currentLogLine;
                        currentLogLine = new LogEntry(currentLine);
                    }
                    else
                    {
                        currentLogLine.AddLine(currentLine);
                    }
                }

                yield return currentLogLine;
            }
        }
    }
}
