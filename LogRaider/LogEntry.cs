using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogRaider
{
    public class LogEntry
    {
        private const int LevelOffset = 32;
        private const int LevelLength = 8;
        private const int MessageOffset = 130;

        private static readonly Regex LogLineRegex = new Regex(@"^\d\d-\d\d-\d\d\d\d \d\d:\d\d:\d\d,\d\d\d", RegexOptions.Compiled);
        public static bool IsLogLine(string line) => LogLineRegex.IsMatch(line) && line.Length > MessageOffset;

        private readonly string _firstLine;
        private readonly List<string> _otherLines = new List<string>();

        public DateTime DateTime { get; }

        public string Message { get; }

        public LogEntry(string firstLine)
        {
            _firstLine = firstLine;

            Message = GetMessage();
            DateTime = GetDateTime();
        }

        public void AddLine(string line) => _otherLines.Add(line);

        public bool IsOutOfMemory() => _otherLines.FirstOrDefault()?.Contains("System.OutOfMemoryException") ?? false;

        public string GetlastInterestingLine()
        {
            for (int i = _otherLines.Count - 1; i >= 0; i--)
            {
                var line = _otherLines[i];
                if (line.StartsWith("   at Engie.") && !line.StartsWith("   at Engie.Prosper.UI.Web.WebForm.ProsperBasePage.OnLoad(EventArgs e)"))
                {
                    return line;
                }
            }

            if (_otherLines.Count < 2 || _otherLines.Last() != "   --- End of inner exception stack trace ---")
            {
                return _otherLines.LastOrDefault();
            }
            else
            {
                return _otherLines[_otherLines.Count - 2];
            }
        }

        //05-02-2018 08:19:33,226 [268  ] DEBUG   Business.Managers.PricingManager         [PutPricingResultInCache            ] *MRB     * Mise en cache d'un PricingResult 225540 sous le nom PricingResult_225540_F_F_True_True_False.
        private DateTime GetDateTime() => DateTime.ParseExact(_firstLine.Substring(0, 23), "dd-MM-yyyy HH:mm:ss,fff", CultureInfo.InvariantCulture);

        private int GetThread() => int.Parse(GetTrimedFirstLineSubString(25, 5));

        public bool IsLevel(string level) => _firstLine.Substring(LevelOffset, LevelLength).StartsWith(level, StringComparison.Ordinal);

        public string GetOriginClass() => GetTrimedFirstLineSubString(40, 41);

        public string GetOriginMethod() => GetTrimedFirstLineSubString(82, 35);

        private string GetUser() => GetTrimedFirstLineSubString(120, 8);

        private string GetMessage() => _firstLine.Substring(MessageOffset);

        private string GetTrimedFirstLineSubString(int startIndex, int length)
            => _firstLine.Substring(startIndex, Math.Min(length, _firstLine.Length - startIndex)).Trim();
        private string GetTrimedFirstLineSubString(int startIndex)
            => _firstLine.Substring(startIndex).Trim();
    }
}
