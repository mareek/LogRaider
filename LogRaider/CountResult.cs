using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogRaider
{
    public struct CountResult
    {
        public string Label { get; }

        public int Count { get; }

        public CountResult(string label, int count)
        {
            Label = label;
            Count = count;
        }

        public static CountResult FromGrouping<T, U>(IGrouping<T, U> group) => new CountResult(group.Key.ToString(), group.Count());

        public static string FormatResult(IList<CountResult> countResults)
        {
            const int maxLabelLength = 100;
            const int maxLineLength = 185;

            if (!countResults.Any())
            {
                return string.Empty;
            }

            var maxValue = countResults.Max(c => c.Count);

            var valueLegnth = maxValue.ToString().Length;
            var labelLength = Math.Min(countResults.Max(c => c.Label.Length), maxLabelLength);
            int barLength = maxLineLength - 6 - valueLegnth - labelLength;

            string formatLabel(CountResult cr) => cr.Label.PadRight(labelLength).Substring(0, labelLength);
            string formatCount(CountResult cr) => cr.Count.ToString().PadLeft(valueLegnth);
            string formatBar(CountResult cr) => (maxValue == 0) ? "" : new string('-', barLength * cr.Count / maxValue);

            return string.Join("\r\n", countResults.Select(cr => $"{formatLabel(cr)} : { formatCount(cr)} |{formatBar(cr)}|"));
        }
    }
}
