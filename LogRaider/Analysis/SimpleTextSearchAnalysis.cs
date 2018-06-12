using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace LogRaider.Analysis
{
    public class SimpleTextSearchAnalysis : ILogAnalysis
    {
        private readonly string _searchTerm;
        private readonly bool _fullMessageSearch;

        public SimpleTextSearchAnalysis(string searchTerm, bool fullMessageSearch)
        {
            _searchTerm = RemoveDiacritics(searchTerm);
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

        public bool Filter(LogEntry logEntry) => ContainsTextSeached(logEntry.Message)
                                                 || _fullMessageSearch && logEntry.OtherLines.Any(ContainsTextSeached);

        private bool ContainsTextSeached(string input) => RemoveDiacritics(input).IndexOf(_searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>
        /// Remove the diacritics (é->e, ç->c, ä->a, etc.) from a string
        /// Code courtesy of the late Michael Kaplan : http://archives.miloush.net/michkap/archive/2007/05/14/2629747.html
        /// </summary>
        static string RemoveDiacritics(string stIn)
        {
            string stFormD = stIn.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();

            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }

            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
    }
}
