using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RingBufferTest
{
    [TestClass]
    class ConcurrentReadWriteBenchmarkTest
    {
        [TestMethod]
        [DataRow(10000000, 10000)]
        [DataRow(10000000, 100)]
        [DataRow(10000000, 1)]
        [DataRow(10000, 100000)]
        public void BenchmarkOne(int testCount, int bufferSize)
        {
            // РЕШЕНО: (проблема была в тесте, пока костыль)
            // Выявлена проблема: возможна ситуация при которой ровно один буфер будет "теряется"
            // чтение/запись по очереди: проблему не решают
            // увеличение время на чтение: проблему не решают
            // уменьшение количества запросов (testCount): усугубляет проблему
            // Уточнение: не вычитывается последний блок коллекции

            int number = 0;
            int deqYieldCount = 0;
            int enqFaults = 0;
            var q = new RingBuffer.Queue<long>(bufferSize);
            var cq = new RingBuffer.ConcurrentQueue<long>(q);
            List<long> listDeq = new(testCount);
            List<long> listEnq = new(testCount);
            Stopwatch stopWatch = new();
            stopWatch.Start();

            Parallel.Invoke(
            () =>
            {
                int i = 0;
                while (i < testCount)
                {
                    bool result = cq.TryEnq(number);
                    if (result)
                    {
                        listEnq.Add(number);
                        number++;
                    }
                    else
                    {
                        enqFaults++;
                    }

                    i++;
                }

                // TODO: перепиши костыль
                // Читаем последний блок для теста

                if (listDeq.Count < listEnq.Count)
                {
                    foreach (var a in cq.TryDeqAll())
                    {
                        listDeq.Add(a);
                    }
                }
            },

            () =>
            {
                for (int i = 0; i < testCount; i++)
                {
                    foreach (var a in cq.TryDeqAll())
                    {
                        listDeq.Add(a);
                        i++;
                    }
                    deqYieldCount++;
                }
            });

            stopWatch.Stop();
            var time = stopWatch.Elapsed.TotalSeconds;
            int repeats = 0;
            int faults = 0;

            for (int i = 1; i < listDeq.Count; i++)
            {
                if (listDeq[i] == listDeq[i - 1]) repeats++;

                if (listDeq[i] != listDeq[i - 1] + 1)
                {
                    faults++;
                    i++;
                }
            }

            Console.WriteLine($"Dequeue requests per second: {Math.Round(listDeq.Count / time, 0)}");
            Console.WriteLine($"Total dequeue requests: {listDeq.Count}");
            Console.WriteLine($"Total enqueue requests: {listEnq.Count}");

            bool enqEqualsDeq = listEnq.SequenceEqual(listDeq);

            Assert.IsTrue(enqEqualsDeq);
            Assert.IsTrue(faults + repeats == 0);
            Assert.IsTrue(listEnq.Count == listDeq.Count);
        }

        [TestMethod]
        [DataRow(10000000, 10000)]
        [DataRow(10000000, 100)]
        [DataRow(10000000, 1)]
        [DataRow(10000, 100000)]
        public void BenchmarkTwo(int testCount, int bufferSize)
        {
            // РЕШЕНО: (использовал Yield и счетчик попыток)
            // Выявлена проблема: редкие просадки производительности в 5 раз
            // Возможно, стоит поменять lock-free блокировку на SpinLock
            // Можно "усреднить" производительность, добавив счетчик попыток взять блокировку

            int errorCount = 0;
            var q = new RingBuffer.Queue<long>(bufferSize);
            var cq = new RingBuffer.ConcurrentQueue<long>(q);
            Stopwatch sw = new();
            sw.Start();

            var consumer = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq.TryEnq(i))
                    {
                    }
                }
            });

            var producer = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        errorCount++;
                    }
                }
            });

            Task.WaitAll(new[] { consumer, producer });

            sw.Stop();

            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.WriteLine($"{Math.Round(testCount / sw.Elapsed.TotalSeconds, 0)} request per second" + "\n");

            Assert.AreEqual(0, errorCount);
        }
    }
}