using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Benchmark
{
    /// <summary>
    /// Нагрузочные тесты чтения/записи
    /// Один поток пишет, другой читает
    /// Тест выделен в проект для удобства отладки
    /// </summary>
    class BenchmarkTest
    {
        // Увеличение времени работы для профилировки
        private const int C = 1;

        static void Main(string[] args)
        {
            TryChannel().Wait();

            Console.WriteLine("High load start");
            HighLoadBenchmarkOne();
            HighLoadBenchmarkTwo();
            Console.WriteLine("High load end" + "\n");

            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Test {i} start...");
                TryDeqBenchmark();
                TryDeqAllBenchmark();
                Console.WriteLine("\n");
            }
        }

        /// <summary>
        /// Бенчмарк метода TryDeq
        /// </summary>
        /// <returns></returns>
        static void TryDeqBenchmark()
        {
            // РЕШЕНО: (использовал Yield и счетчик попыток)
            // Выявлена проблема: редкие просадки производительности в 5 раз
            // Возможно, стоит поменять lock-free блокировку на SpinLock
            // Можно "усреднить" производительность, добавив счетчик попыток взять блокировку

            int testCount = 10000000 * C;
            int bufferSize = 10000;
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
                        throw new Exception("ERROR");
                    }
                }
            });

            Task.WaitAll(new[] { consumer, producer });

            sw.Stop();

            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.WriteLine($"{Math.Round(testCount / sw.Elapsed.TotalSeconds, 0)} request per second" + "\n");
        }

        /// <summary>
        /// Бенчмарк метода TryDeqAll
        /// </summary>
        /// <returns></returns>
        static void TryDeqAllBenchmark()
        {
            // Проблема: не вычитывается последний блок коллекции, решено "костылём"

            int testCount = 10000000 * C;
            int bufferSize = 10000;
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

            var r = listEnq.SequenceEqual(listDeq);
            Console.WriteLine($"Read/write equals: {r}");
            Console.WriteLine($"Deq requests per second: {Math.Round(listDeq.Count / time, 0)}");
            Console.WriteLine($"Enq requests per second: {Math.Round(listEnq.Count / time, 0)}");

            Console.WriteLine($"Pieces: {deqYieldCount}");
            Console.WriteLine($"Enq faults: { enqFaults}");
            Console.WriteLine($"Repeats: {(double)repeats / listDeq.Count} ({repeats})");
            Console.WriteLine($"Faults: {(double)faults / listDeq.Count} ({faults})");

            Console.WriteLine($"Deq count: {listDeq.Count}");
            Console.WriteLine($"Enq count: {listEnq.Count}");
            Console.WriteLine($"Total time: {time}");
            Console.WriteLine($"Total calls: {testCount}");

            if (listDeq.Count != listEnq.Count || listDeq.Count != write || faults + repeats > 0)
            {
                throw new Exception("TRY-ALL ERROR");
            }
        }

        // Код использования Channel
        // Взят с сайта: https://deniskyashif.com/2019/12/08/csharp-channels-part-1/
        //

        public static async Task TryChannel()
        {
            int testCount = 10000000 * C;
            int bufferSize = 10000;
            var ch = Channel.CreateBounded<long>(bufferSize);
            Stopwatch stopWatch = new();
            stopWatch.Start();

            var consumer = Task.Run(async () =>
            {
                while (await ch.Reader.WaitToReadAsync())
                    await ch.Reader.ReadAsync();
            });
            var producer = Task.Run(async () =>
            {
                var rnd = new Random();
                for (int i = 0; i < testCount; i++)
                {
                    await ch.Writer.WriteAsync(i);
                }
                ch.Writer.Complete();
            });

            await Task.WhenAll(producer, consumer);
            stopWatch.Stop();
            var time = stopWatch.Elapsed.TotalSeconds;

            Console.WriteLine("System.Threading.Channels performance: " + Math.Round(testCount / time, 0) + "\n");
        }

        /// <summary>
        /// NO DRY CODE
        /// Нагрузочный тест на 4х разных очередях
        /// </summary>
        static void HighLoadBenchmarkOne()
        {
            int testCount = 10000000 * C;
            int bufferSize = 10000;
            var q1 = new RingBuffer.Queue<long>(bufferSize);
            var cq1 = new RingBuffer.ConcurrentQueue<long>(q1);
            var q2 = new RingBuffer.Queue<long>(bufferSize);
            var cq2 = new RingBuffer.ConcurrentQueue<long>(q1);
            var q3 = new RingBuffer.Queue<long>(bufferSize);
            var cq3 = new RingBuffer.ConcurrentQueue<long>(q1);
            var q4 = new RingBuffer.Queue<long>(bufferSize);
            var cq4 = new RingBuffer.ConcurrentQueue<long>(q1);
            Stopwatch sw = new();
            sw.Start();

            var consumer1 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq1.TryEnq(i))
                    {
                    }
                }
            });
            var consumer2 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq2.TryEnq(i))
                    {
                    }
                }
            });
            var consumer3 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq3.TryEnq(i))
                    {
                    }
                }
            });
            var consumer4 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq4.TryEnq(i))
                    {
                    }
                }
            });

            var producer1 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq1.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        throw new Exception("ERROR1");
                    }
                }
            });
            var producer2 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq2.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        throw new Exception("ERROR2");
                    }
                }
            });
            var producer3 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq3.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        throw new Exception("ERROR3");
                    }
                }
            });
            var producer4 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq4.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        throw new Exception("ERROR4");
                    }
                }
            });

            Task.WaitAll(new[] { consumer1, producer1, consumer2, producer2, consumer3, producer3, consumer4, producer4 });

            sw.Stop();

            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.WriteLine($"{Math.Round(testCount / sw.Elapsed.TotalSeconds, 0)} request per second");
        }

        /// <summary>
        /// NO DRY CODE
        /// Нагрузочный тест на одной очереди для 4х поставщиков и 4х потребителей
        /// </summary>
        static void HighLoadBenchmarkTwo()
        {
            int testCount = 10000000 * C;
            int bufferSize = 10000;
            var q1 = new RingBuffer.Queue<long>(bufferSize);
            var cq1 = new RingBuffer.ConcurrentQueue<long>(q1);
            Stopwatch sw = new();
            sw.Start();

            var consumer1 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq1.TryEnq(i))
                    {
                    }
                }
            });
            var consumer2 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq1.TryEnq(i))
                    {
                    }
                }
            });
            var consumer3 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq1.TryEnq(i))
                    {
                    }
                }
            });
            var consumer4 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    while (!cq1.TryEnq(i))
                    {
                    }
                }
            });

            var producer1 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq1.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        //throw new Exception("ERROR1");
                    }
                }
            });
            var producer2 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq1.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        //throw new Exception("ERROR2");
                    }
                }
            });
            var producer3 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq1.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        //throw new Exception("ERROR3");
                    }
                }
            });
            var producer4 = Task.Run(() =>
            {
                for (long i = 0; i < testCount; i++)
                {
                    long j;
                    while (!cq1.TryDeq(out j))
                    {
                    }

                    if (j != i)
                    {
                        //throw new Exception("ERROR4");
                    }
                }
            });

            Task.WaitAll(new[] { consumer1, producer1, consumer2, producer2, consumer3, producer3, consumer4, producer4 });

            sw.Stop();

            Console.WriteLine(sw.Elapsed.TotalSeconds);
            Console.WriteLine($"{Math.Round(testCount / sw.Elapsed.TotalSeconds, 0)} request per second");
        }
    }
}