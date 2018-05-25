using System;
using System.Collections.Generic;
using System.Linq;

namespace LogRaider
{
    class PricingCacheAnalysis
    {
        public IEnumerable<CountResult> GetCacheUsageDurations(IEnumerable<LogEntry> logEntries) 
            => GetCacheUsage(logEntries).SelectMany(c => c.GetHitsDurations())
                                        .GroupBy(d => (int)d.TotalMinutes)
                                        .OrderByDescending(g => g.Key)
                                        .Select(CountResult.FromGrouping);

        private static IEnumerable<CacheEntry> GetCacheUsage(IEnumerable<LogEntry> logEntries)
        {
            var cacheMisses = new List<LogEntry>();
            var cacheEntries = new List<CacheEntry>();
            var lastCacheEntryByKey = new Dictionary<string, CacheEntry>();
            foreach (var logEntry in logEntries)
            {
                if (CacheEntry.IsCacheFill(logEntry))
                {
                    var cacheEntry = new CacheEntry(logEntry);
                    cacheEntries.Add(cacheEntry);
                    lastCacheEntryByKey[cacheEntry.Key] = cacheEntry;
                }
                else if (CacheEntry.IsCacheHit(logEntry) && lastCacheEntryByKey.TryGetValue(CacheEntry.GetKeyFromCacheHit(logEntry), out var cacheEntry))
                {
                    cacheEntry.AddHit(logEntry);
                }
                //else if (CacheEntry.IsCacheMiss(logEntry))
                //{
                //    cacheMisses.Add(logEntry);
                //}
            }

            //var cacheMissesByKey = cacheMisses.ToLookup(CacheEntry.GetKeyFromCacheMiss);

            //foreach (var cacheEntry in cacheEntries)
            //{
                
            //}

            return cacheEntries;
        }

        private class CacheEntry
        {
            public string Key { get; }

            private readonly DateTime _usageStart;

            private readonly List<DateTime> _hitDates = new List<DateTime>();

            private DateTime _usageEnd;

            public int UseCount => _hitDates.Count;

            public CacheEntry(LogEntry cacheFillEntry)
            {
                Key = GetKeyFromCacheFill(cacheFillEntry);
                _usageStart = cacheFillEntry.DateTime;
                _usageEnd = cacheFillEntry.DateTime;
            }

            public void AddHit(LogEntry logEntry)
            {
                _hitDates.Add(logEntry.DateTime);
                if (logEntry.DateTime > _usageEnd)
                {
                    _usageEnd = logEntry.DateTime;
                }
            }

            public TimeSpan GetTotalDuration() => _usageEnd - _usageStart;

            public IEnumerable<TimeSpan> GetHitsDurations() => _hitDates.Select(d => d - _usageStart);

            //05-02-2018 08:19:33,226 [268  ] DEBUG   Business.Managers.PricingManager         [PutPricingResultInCache            ] *MRB     * Mise en cache d'un PricingResult 225540 sous le nom PricingResult_225540_F_F_True_True_False.
            public static bool IsCacheFill(LogEntry logEntry) => logEntry.GetOriginClass() == "Business.Managers.PricingManager" && logEntry.GetOriginMethod() == "PutPricingResultInCache";
            private static string GetKeyFromCacheFill(LogEntry logEntry) => logEntry.Message.Split(new[] { " sous le nom ", "." }, StringSplitOptions.None)[1];

            //05-02-2018 08:19:33,898 [268  ] DEBUG   Business.Managers.PricingManager         [GetPricingResultFromCache          ] *MRB     * Récupération du PricingResult PricingResult_225540_F_F_True_True_False dans le cache.
            public static bool IsCacheHit(LogEntry logEntry) => logEntry.GetOriginClass() == "Business.Managers.PricingManager" && logEntry.GetOriginMethod() == "GetPricingResultFromCache" && !logEntry.Message.StartsWith("Pas");
            public static string GetKeyFromCacheHit(LogEntry logEntry) => logEntry.Message.Split(new[] { "du PricingResult ", " dans le cache." }, StringSplitOptions.None)[1];

            //05-02-2018 08:19:29,288 [268  ] DEBUG   Business.Managers.PricingManager         [GetPricingResultFromCache          ] *MRB     * Pas de PricingResult PricingResult_225540_F_F_True_True_False dans le cache.
            public static bool IsCacheMiss(LogEntry logEntry) => logEntry.GetOriginClass() == "Business.Managers.PricingManager" && logEntry.GetOriginMethod() == "GetPricingResultFromCache" && logEntry.Message.StartsWith("Pas");
            public static string GetKeyFromCacheMiss(LogEntry logEntry) => logEntry.Message.Split(new[] { "du PricingResult ", " dans le cache." }, StringSplitOptions.None)[1];

        }
    }
}
