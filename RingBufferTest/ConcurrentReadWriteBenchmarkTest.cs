using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RingBuffer
{
    [TestClass]
    public class ConcurrentReadWriteBenchmarkTest
    {
        [TestMethod]
        [DataRow(10000000, 10000)]
        [DataRow(10000000, 100)]
        [DataRow(10000000, 1)]
        [DataRow(10000, 100000)]
        public void BenchmarkOne(int testCount, int bufferSize)
        {
            // Проблема: не вычитывается последний блок коллекции, решено "костылём"
            // Лямбды с замыканиями (в т.ч. вызывается метод с итератором), потому постоянно аллоцируется память
            // Тестируется метод TryDeqAllLazy()

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

                finalConsumption();
            },

            () =>
            {
                for (int i = 0; i < testCount; i++)
                {
                    foreach (var a in cq.TryDeqAllLazy())
                    {
                        listDeq.Add(a);
                        i++;
                    }
                    deqYieldCount++;
                }
            });

            void finalConsumption()
            {
                if (listDeq.Count < listEnq.Count)
                {
                    foreach (var a in cq.TryDeqAllLazy())
                    {
                        listDeq.Add(a);
                    }
                }
            };

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

        [TestMethod]
        [DataRow(10000000, 10000)]
        [DataRow(10000000, 100)]
        [DataRow(10000000, 1)]
        [DataRow(10000, 100000)]
        [DataRow(30000000, 10000)]
        public void BenchmarkThree(int testCount, int bufferSize)
        {
            // В тесте убраны замыкания из лямбд
            // Тестируется TryDeqAll() без итератора
            // Метод показывает более высокую производительность, чем TryDeqAllLazy()

            var q = new RingBuffer.Queue<long>(bufferSize);
            var cq = new RingBuffer.ConcurrentQueue<long>(q);
            List<long> listDeq;
            List<long> listEnq;
            Stopwatch stopWatch = new();
            stopWatch.Start();

            // TODO: перепиши костыль
            // Читаем последний блок для теста

            void finalConsumption()
            {
                if (listDeq.Count < listEnq.Count)
                {
                    foreach (var a in cq.TryDeqAllLazy())
                    {
                        listDeq.Add(a);
                    }
                }
            };

            var producer = Task.Run(() =>
            {
                List<long> listEnq = new(testCount);
                int enqFaults = 0;
                int write = 0;
                int i = 0;
                while (i < testCount)
                {
                    bool result = cq.TryEnq(write);
                    if (result)
                    {
                        listEnq.Add(write);
                        write++;
                    }

                    if (!result)
                    {
                        enqFaults++;
                    }

                    i++;
                }

                return (listEnq, enqFaults, write);
            });

            var consumer = Task.Run(() =>
            {
                List<long> listDeq = new(testCount);
                int deqYieldCount = 0;

                for (int i = 0; i < testCount; i++)
                {
                    int initialCount = listDeq.Count;

                    cq.TryDeqAll(listDeq);

                    i += initialCount - listDeq.Count;

                    deqYieldCount++;
                }
                return (listDeq, deqYieldCount);
            });

            int enqFaults = producer.Result.enqFaults;
            int write = producer.Result.write;
            int deqYieldCount = consumer.Result.deqYieldCount;
            listEnq = producer.Result.listEnq;
            listDeq = consumer.Result.listDeq;

            finalConsumption();

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

            if (listDeq.Count != listEnq.Count || listDeq.Count != write || faults + repeats > 0)
            {
                throw new Exception("TRY-ALL ERROR");
            }

            bool enqEqualsDeq = listEnq.SequenceEqual(listDeq);
            Assert.IsTrue(enqEqualsDeq);
            Assert.IsTrue(faults + repeats == 0);
            Assert.IsTrue(listEnq.Count == listDeq.Count);
        }
    }
}