using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LogRaider
{
    public class LogDirectory
    {
        private readonly DirectoryInfo _directory;

        public LogDirectory(DirectoryInfo directory) => _directory = directory;

        private IEnumerable<LogFile> GetLogFiles() => _directory.EnumerateFiles().Select(f => new LogFile(f));

        public long GetSize() => GetLogFiles().Sum(f => f.GetLogFileSize());

        public IEnumerable<LogEntry> ReadSequential(Func<LogEntry, bool> filter = null) => GetLogFiles().SelectMany(f => f.Read(filter ?? DefaultFilter));

        public IEnumerable<LogEntry> ReadParallel(Func<LogEntry, bool> filter = null) => SelectManyParallel(GetLogFiles(), lf => lf.Read(filter ?? DefaultFilter));

        private bool DefaultFilter(LogEntry _) => true;

        private static IEnumerable<TResult> SelectManyParallel<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            => SelectManyParallel(source, selector, 1_000_000, Environment.ProcessorCount - 1);

        private static IEnumerable<TResult> SelectManyParallel<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector, int resultBufferSize, int nbProducers)
        {
            var resultCollection = new BlockingCollection<TResult>(resultBufferSize);
            var sourcessQueue = new ConcurrentQueue<TSource>(source);

            void ProduceResults()
            {
                while (sourcessQueue.TryDequeue(out var element) || !sourcessQueue.IsEmpty)
                {
                    foreach (var result in selector(element))
                    {
                        resultCollection.Add(result);
                    }
                }
            };

            var producers = new Task[nbProducers];

            for (int i = 0; i < nbProducers; i++)
            {
                producers[i] = Task.Run(ProduceResults);
            }

            Task.Run(() =>
            {
                Task.WaitAll(producers);
                resultCollection.CompleteAdding();
            });

            return resultCollection.GetConsumingEnumerable();
        }
    }
}
