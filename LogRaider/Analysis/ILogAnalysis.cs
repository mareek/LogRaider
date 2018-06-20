using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogRaider.Analysis
{
    public interface ILogAnalysis
    {
        string Name { get; }

        bool CanBeParalelyzed { get; }

        string AnalyseLogs(IEnumerable<LogEntry> logEntries);

        bool Filter(LogEntry logEntry);
    }
}
