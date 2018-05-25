using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogRaider.Analysis
{
    public class ExceptionByDateAnalysis : ILogAnalysis
    {
        public string Name => $"{_exceptionName} par jour";

        private readonly string _exceptionName;

        public ExceptionByDateAnalysis(string exceptionName)
        {
            _exceptionName = exceptionName;
        }

        public bool Filter(LogEntry logEntry) => true;

        public static ExceptionByDateAnalysis Create<T>() => new ExceptionByDateAnalysis(typeof(T).FullName);

        public string AnalyseLogs(IEnumerable<LogEntry> logEntries)
        {
            var countByDate = GetExceptionCountByDate(logEntries, out var start, out var end);

            if (!countByDate.Any())
            {
                return $"l'exception {_exceptionName} n'apparait pas dans les logs";
            }
            else
            {
                var resultBuilder = new StringBuilder();
                resultBuilder.AppendLine(string.Join("\t", "Date", "Count"));

                var currentDate = start;
                while (currentDate <= end)
                {
                    resultBuilder.AppendLine(string.Join("\t", currentDate.ToString("yyyy-MM-dd"), countByDate.TryGetValue(currentDate, out int count) ? count : 0));
                    currentDate = currentDate.AddDays(1);
                }

                return resultBuilder.ToString();
            }
        }

        private ConcurrentDictionary<DateTime, int> GetExceptionCountByDate(IEnumerable<LogEntry> logEntries, out DateTime start, out DateTime end)
        {
            start = DateTime.MaxValue;
            end = DateTime.MinValue;
            var countByDate = new ConcurrentDictionary<DateTime, int>();
            foreach (var entry in logEntries)
            {
                var entryDate = entry.DateTime.Date;

                start = (entryDate < start) ? entryDate : start;
                end = (end < entryDate) ? entryDate : end;

                if (IsExceptionEntry(entry))
                {
                    countByDate.AddOrUpdate(entryDate, 1, (_, count) => count + 1);
                }
            }

            return countByDate;
        }

        //11-04-2018 14:37:36,722 [138  ] ERROR   Supervision.Exceptions.ExceptionManager  [Trace                              ] *MOA     * Exception of type 'System.OutOfMemoryException' was thrown.
        private bool IsExceptionEntry(LogEntry entry) => entry.Message.StartsWith($"Exception of type '{_exceptionName}'", StringComparison.Ordinal);
    }
}
