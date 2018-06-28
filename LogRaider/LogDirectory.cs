﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LogRaider.Configuration;

namespace LogRaider
{
    public class LogDirectory
    {
        private readonly LogConfiguration _conf;
        private readonly DirectoryInfo _directory;

        private FileInfo ConfFile => new FileInfo(Path.Combine(_directory.FullName, "conf.xml"));

        public LogDirectory(DirectoryInfo directory)
        {
            _directory = directory;
            _conf = LoadConf();
        }

        private LogConfiguration LoadConf() => ConfFile.Exists ? ConfFile.DeserializeFromXml<LogConfiguration>() : LogConfiguration.GetDefault();

        private void SaveConf() => _conf.SerializeToXmlFile(ConfFile);

        private IEnumerable<LogFile> GetLogFiles() => from file in _directory.EnumerateFiles()
                                                      where LogFile.IsLogFile(file) || ZipService.IsZipFile(file)
                                                      select new LogFile(file);

        public long GetSize() => GetLogFiles().Sum(f => f.GetLogFileSize());

        public IEnumerable<LogEntry> ReadSequential(Func<LogEntry, bool> filter = null) => GetLogFiles().SelectMany(f => f.Read(filter ?? DefaultFilter));

        public IEnumerable<LogEntry> ReadParallel(Func<LogEntry, bool> filter = null) => GetLogFiles().SelectManyParallel(lf => lf.Read(filter ?? DefaultFilter));

        private bool DefaultFilter(LogEntry _) => true;

        public void Download()
        {
            var source = _conf.Source;
            switch (source.Type)
            {
                case LogSource.PathType.File:
                    DownloadFromDirectory();
                    break;
                case LogSource.PathType.Url:
                default:
                    throw new NotSupportedException($"Unsupported path type : {source.Type}");
            }

            _conf.LastDownloadDate = DateTime.Today;
            SaveConf();
        }

        private void DownloadFromDirectory()
        {
            var sourceDirectory = new DirectoryInfo(_conf.Source.Path);
            foreach (var logFile in GetFilesToDownload(sourceDirectory, _conf.LastDownloadDate, _conf.FileDateFormat))
            {
                logFile.CopyTo(Path.Combine(_directory.FullName, logFile.Name), true);
            }
        }

        private IEnumerable<FileInfo> GetFilesToDownload(DirectoryInfo sourceDirectory, DateTime lastDownloadDate, string fileDateFormat)
        {
            var currentDate = lastDownloadDate.Date;
            while (currentDate < DateTime.Today)
            {
                var logFile = new FileInfo(Path.Combine(sourceDirectory.FullName, currentDate.ToString(fileDateFormat)));
                if (logFile.Exists)
                {
                    yield return logFile;
                }

                currentDate = currentDate.AddDays(1);
            }

            yield return new FileInfo(Path.Combine(sourceDirectory.FullName, "Debug.log"));
        }
    }
}
