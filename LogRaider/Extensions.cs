using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace LogRaider
{
    public static class Extensions
    {
        public static IEnumerable<TResult> SelectManyParallel<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector)
            => SelectManyParallel(source, selector, 1_000_000, Environment.ProcessorCount - 1);

        public static IEnumerable<TResult> SelectManyParallel<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TResult>> selector, int resultBufferSize, int nbProducers)
        {
            var resultCollection = new BlockingCollection<TResult>(resultBufferSize);
            var sourcesQueue = new ConcurrentQueue<TSource>(source);

            void ProduceResults()
            {
                while (sourcesQueue.TryDequeue(out var element) || !sourcesQueue.IsEmpty)
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

        public static void SerializeToXmlFile<T>(this T objectoToserialize, FileInfo file)
        {
            using (var textWriter = new StreamWriter(file.FullName))
            {
                SerializeToXml(objectoToserialize, textWriter);
            }
        }

        private static void SerializeToXml<T>(T objectoToserialize, TextWriter textWriter) => new XmlSerializer(typeof(T)).Serialize(textWriter, objectoToserialize);

        public static T DeserializeFromXml<T>(this FileInfo file)
        {
            using (var textReader = file.OpenText())
            {
                return (T)new XmlSerializer(typeof(T)).Deserialize(textReader);
            }
        }
    }
}
