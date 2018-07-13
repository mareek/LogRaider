using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LogRaider.Analysis
{
    public class ErrorByCategoryAnalysis : ILogAnalysis
    {
        public string Name => nameof(ErrorByCategoryAnalysis);

        public bool CanBeParalelyzed => true;

        public string AnalyseLogs(IEnumerable<LogEntry> logEntries)
        {
            var lineCountByKey = new ConcurrentDictionary<string, int>();
            foreach (var logLine in logEntries)
            {
                lineCountByKey.AddOrUpdate(GetCategory(logLine), 1, (_, v) => v + 1);
            }

            return string.Join("\r\n", lineCountByKey.OrderByDescending(e => e.Value)
                                                     .ThenBy(e => e.Key)
                                                     .Select(e => $"{ e.Value}\t{e.Key}"));
        }

        public bool Filter(LogEntry logEntry) => logEntry.IsLevel("ERROR");

        private string GetCategory(LogEntry logEntry) => logEntry.OtherLines.FirstOrDefault() ?? logEntry.Message;
    }
}
