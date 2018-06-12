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

        private bool IsZipFile() => ZipService.IsZipFile(_file);

        public IEnumerable<LogEntry> Read(Func<LogEntry, bool> filter) => IsZipFile() ? ReadZipLogFile(filter) : ReadNonZipLogFile(filter);

        private IEnumerable<LogEntry> ReadZipLogFile(Func<LogEntry, bool> filter)
        {
            using (var zipArchive = ZipFile.OpenRead(_file.FullName))
            {
                foreach (var zipEntry in zipArchive.Entries)
                {
                    foreach (var logEntry in ReadLogStream(zipEntry.Open()).Where(filter))
                    {
                        yield return logEntry;
                    }
                }
            }
        }

        private IEnumerable<LogEntry> ReadNonZipLogFile(Func<LogEntry, bool> filter)
        {
            foreach (var logEntry in ReadLogStream(_file.OpenRead()).Where(filter))
            {
                yield return logEntry;
            }
        }

        public long GetLogFileSize() => IsZipFile() ? GetUncompressedZipFileSize() : _file.Length;

        private long GetUncompressedZipFileSize()
        {
            using (var zipArchive = ZipFile.OpenRead(_file.FullName))
            {
                return zipArchive.Entries.Sum(e => e.Length);
            }
        }

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
