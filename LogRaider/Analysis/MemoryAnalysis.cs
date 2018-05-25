using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogRaider.Analysis
{
    public class MemoryAnalysis : ILogAnalysis
    {
        public string Name => "Analyse mémoire";

        public bool Filter(LogEntry logEntry) => MemoryEntry.IsMemoryEntry(logEntry);

        public string AnalyseLogs(IEnumerable<LogEntry> logEntries)
        {
            var resultBuilder = new StringBuilder();
            resultBuilder.AppendLine(string.Join("\t", "Heure", "Private memory", "Working set", "GC total memory"));

            var memoryEntriesByMinute = NormalizeByMinute(logEntries).ToList();
            if (memoryEntriesByMinute.Any())
            {
                bool spansMultipleDays = (memoryEntriesByMinute.First().DateTime.Date != memoryEntriesByMinute.Last().DateTime.Date);
                var dateFormat = spansMultipleDays ? "s" : "HH:mm:ss";
                foreach (var memoryEntry in memoryEntriesByMinute)
                {
                    resultBuilder.AppendLine(string.Join("\t", memoryEntry.DateTime.ToString(dateFormat), memoryEntry.PrivateMemory, memoryEntry.WorkingSet, memoryEntry.GcTotalMemory));
                }
            }

            return resultBuilder.ToString();
        }

        private static IEnumerable<MemoryEntry> NormalizeByMinute(IEnumerable<LogEntry> logEntries)
        {
            var entriesByMinute = GetEntriesByMinute(logEntries);
            if (!entriesByMinute.Any())
            {
                yield break;
            }

            var end = entriesByMinute.Max(g => g.Key);
            var currDate = entriesByMinute.Min(g => g.Key);
            MemoryEntry lastEntry = null;
            while (currDate <= end)
            {
                if (entriesByMinute.TryGetValue(currDate, out var entry))
                {
                    lastEntry = entry;
                }

                yield return new MemoryEntry(currDate, lastEntry.PrivateMemory, lastEntry.WorkingSet, lastEntry.GcTotalMemory);

                currDate = currDate.AddMinutes(1);
            }
        }

        private static ConcurrentDictionary<DateTime, MemoryEntry> GetEntriesByMinute(IEnumerable<LogEntry> logEntries)
        {
            DateTime getMinute(DateTime d) => d.Date.AddHours(d.Hour).AddMinutes(d.Minute);

            var entriesByMinute = new ConcurrentDictionary<DateTime, MemoryEntry>();
            foreach (var logEntry in logEntries)
            {
                var entry = entriesByMinute.GetOrAdd(getMinute(logEntry.DateTime), d => new MemoryEntry(d));
                entry.Update(logEntry);
            }

            return entriesByMinute;
        }
    }
}
