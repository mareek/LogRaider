using System;

namespace LogRaider
{
    public class MemoryEntry
    {
        public DateTime DateTime { get; }

        public decimal PrivateMemory { get; private set; }

        public decimal WorkingSet { get; private set; }

        public decimal GcTotalMemory { get; private set; }

        public MemoryEntry(DateTime dateTime) => DateTime = dateTime;

        public MemoryEntry(DateTime dateTime, decimal privateMemory, decimal workingSet, decimal gcTotalMemory)
            : this(dateTime)
        {
            PrivateMemory = privateMemory;
            WorkingSet = workingSet;
            GcTotalMemory = gcTotalMemory;
        }

        public void Update(LogEntry logEntry)
        {
            var (privateMemory, workingSet, gcTotalMemory) = ParseMemoryInfos(logEntry);
            lock (this)
            {
                PrivateMemory = Math.Max(privateMemory, PrivateMemory);
                WorkingSet = Math.Max(workingSet, WorkingSet);
                GcTotalMemory = Math.Max(gcTotalMemory, GcTotalMemory);
            }
        }

        private static (decimal privateMemory, decimal workingSet, decimal gcTotalMemory) ParseMemoryInfos(LogEntry logEntry)
        {
            //MemoryInfos : Private memory: 194,25390625 MB | Working set: 251,53515625 MB  | GC total memory: 44,9638595581055 MB
            var messageParts = logEntry.Message.Split('|');
            return (ParseMemoryMessagePart(messageParts[0]), ParseMemoryMessagePart(messageParts[1]), ParseMemoryMessagePart(messageParts[2]));
        }

        private static decimal ParseMemoryMessagePart(string messagePart)
        {
            // MemoryInfos : Private memory: 194,25390625 MB 
            var startPosition = messagePart.LastIndexOf(':') + 1;
            var endPosition = messagePart.LastIndexOf("MB") - 1;
            var stringToParse = messagePart.Substring(startPosition, endPosition - startPosition);
            return decimal.Parse(stringToParse);
        }

        //18-04-2018 09:40:50,230 [63   ] DEBUG   rosperApp/ZoneAuthenticated/Default.aspx [OnLoad                             ] *MHAA    * MemoryInfos : Private memory: 194,25390625 MB | Working set: 251,53515625 MB  | GC total memory: 44,9638595581055 MB
        public static bool IsMemoryEntry(LogEntry logEntry) => logEntry.IsLevel("DEBUG") && logEntry.Message.StartsWith("MemoryInfos", StringComparison.Ordinal);
    }
}
