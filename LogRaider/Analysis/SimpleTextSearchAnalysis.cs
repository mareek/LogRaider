using System.Collections.Generic;
using System.Linq;

namespace LogRaider.Analysis
{
    public class SimpleTextSearchAnalysis : ILogAnalysis
    {
        private readonly string _searchTerm;

        public SimpleTextSearchAnalysis(string searchTerm) => _searchTerm = searchTerm;

        public string Name => $"recherche de '{_searchTerm}'";

        public string AnalyseLogs(IEnumerable<LogEntry> logEntries) => string.Join("\r\n", logEntries.Select(l => l.DateTime.ToString("s")).Distinct().OrderBy(l => l));

        public bool Filter(LogEntry logEntry) => logEntry.Message.Contains(_searchTerm);
    }
}
