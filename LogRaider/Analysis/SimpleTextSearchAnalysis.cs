using System.Collections.Generic;
using System.Linq;

namespace LogRaider.Analysis
{
    public class SimpleTextSearchAnalysis : ILogAnalysis
    {
        private readonly string _searchTerm;
        private readonly bool _fullMessageSearch;

        public SimpleTextSearchAnalysis(string searchTerm, bool fullMessageSearch)
        {
            _searchTerm = searchTerm;
            _fullMessageSearch = fullMessageSearch;
        }

        public string Name => $"recherche de '{_searchTerm}'";

        public string AnalyseLogs(IEnumerable<LogEntry> logEntries)
        {
            if (string.IsNullOrWhiteSpace(_searchTerm))
            {
                return "Aucun terme sélectionné";
            }
            else
            {
                return CountResult.FormatResult(logEntries.GroupBy(l => l.DateTime.Date).OrderBy(g => g.Key).Select(CountResult.FromGrouping).ToList());
            }
        }

        public bool Filter(LogEntry logEntry) => logEntry.Message.Contains(_searchTerm)
                                                 || _fullMessageSearch && logEntry.OtherLines.Any(l => l.Contains(_searchTerm));
    }
}
