using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Benchmark
{
    /// <summary>
    /// Нагрузочные тесты чтения/записи
    /// Один поток пишет, другой читает
    /// </summary>
    class Program
    {


        static void Main(string[] args)
        {
            for (int i = 0; i < 10; i++)
            {
                Console.WriteLine($"Test {i} start...");
                var r1 = TryDeqBenchmark();
                var r2 = TryDeqAllBenchmark();
                var result = r1 && r2 == true ? "ok" : "falls";
                Console.WriteLine($"Test {i} {result}" + "\n");
            }
        }

        /// <summary>
        /// Бенчмарк метода TryDeq
        /// </summary>
        /// <returns></returns>
        static bool TryDeqBenchmark()
        {
            // Выявлена проблема: редкие просадки производительности в 5 раз
            // Возможно, стоит поменять lock-free блокировку на SpinLock

            int testCount = 10000000;
            int bufferSize = 10000;
            var q = new RingBuffer.Queue<long>(bufferSize);
            var cq = new RingBuffer.ConcurrentQueue<long>(q);

            Stopwatch sw = new();
            sw.Start();

            var consumer = Task.Run(() =>
            {
                for (long i = 0; i < 10000000; i++)
                {
                    while (!cq.TryEnq(i))
                    {
                    }
                }
            });

            var producer = Task.Run(() =>
            {
                for (long i = 0; i < 10000000; i++)
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
            return true;
        }

        /// <summary>
        /// Бенчмарк метода TryDeqAll
        /// </summary>
        /// <returns></returns>
        static bool TryDeqAllBenchmark()
        {
            // Выявлена проблема: возможна ситуация при которой ровно один буфер будет "теряется"
            // чтение/запись по очереди: проблему не решают
            // увеличение время на чтение: проблему не решают
            // уменьшение количества запросов (testCount): усугубляет проблему

            int testCount = 10000000;
            int bufferSize = 10000;
            var q = new RingBuffer.Queue<long>(bufferSize);
            var cq = new RingBuffer.ConcurrentQueue<long>(q);
            List<long> listDeq = new(testCount);
            List<long> listEnq = new(testCount);
            Stopwatch stopWatch = new();
            stopWatch.Start();
            int write = 0;

            int deqYieldCount = 0;
            int enqFaults = 0;

            var producer = Task.Run(() =>
            {
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
            });

            var consumer = Task.Run(() =>
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


            Task.WaitAll(new[] { consumer, producer });

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
            Console.WriteLine($"Total calls: {testCount}" + "\n");

            if (listDeq.Count != listEnq.Count || listDeq.Count != write || faults + repeats > 0)
            {
                Console.WriteLine("TRY-DEQ-ALL BENCHMARK FALL: " + (listEnq.Count - listDeq.Count) + "\n");
                return false;
            }

            return true;
        }
    }
}