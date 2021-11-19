using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RingBuffer
{
    [TestClass]
    public class ConcurrentQueueTest
    {
        private const int TEST_COUNT = 1000;
        private readonly string str1 = "A";
        private readonly string str2 = "B";
        private readonly string str3 = "C";
        private readonly List<Task> taskList = new();

        [TestMethod]
        [DataRow(null)]
        public void ShouldThrowExceptions(Queue<string> queue )
        {
            Assert.ThrowsException<NullReferenceException>(() => new ConcurrentQueue<string>(queue));
            Assert.ThrowsException<ArgumentException>(() => new ConcurrentQueue<string>(new Queue<string>(0)));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(10000)]
        public void ShouldConcurrentQueueIndependentOfQueue(int capacity)
        {
            var q = new Queue<string>(capacity);
            var cq = new ConcurrentQueue<string>(q);

            cq.TryEnq(str1);
            q.Deq(out string result2);
            q = null;
            cq.TryDeq(out string result);
            cq.TryDeq(out result2);

            Assert.AreEqual(result, str1);
            Assert.AreEqual(result2, default);
        }

        [TestMethod]
        [DataRow(3)]
        public void ShouldEnqueueCorrectly(int capacity)
        {
            var q = new Queue<string>(capacity);
            var cq = new ConcurrentQueue<string>(q);

            Assert.IsTrue(cq.TryEnq(str1));
            Assert.IsTrue(cq.TryEnq(str2));
            Assert.IsTrue(cq.TryEnq(str3));
            Assert.IsFalse(cq.TryEnq(str3));
            Assert.IsFalse(cq.TryEnq(str2));
            Assert.IsFalse(cq.TryEnq(str1));

            Assert.IsTrue(cq.GetPrivateArrayCopy().SequenceEqual(new string[] { str1, str2, str3 }));
        }

        [TestMethod]
        [DataRow(3)]
        public void ShouldDequeueCorrectrly(int capacity)
        {
            var q = new Queue<string>(capacity);
            var cq = new ConcurrentQueue<string>(q);
            string empty = default;

            Enumerable.Repeat(1, 10).ToList().ForEach(a => Assert.IsFalse(cq.TryDeq(out string result)));

            Assert.IsTrue(cq.TryEnq(str1));
            Assert.IsTrue(cq.TryEnq(str2));
            Assert.IsTrue(cq.TryEnq(null));
            Assert.IsFalse(cq.TryEnq(str3));

            Assert.IsTrue(cq.TryDeq(out string result));
            Assert.AreEqual(result, str1);
            Assert.IsTrue(cq.TryDeq(out result));
            Assert.AreEqual(result, str2);
            Assert.IsTrue(cq.TryDeq(out result));
            Assert.AreEqual(result, null);
            Assert.IsFalse(cq.TryDeq(out result));

            Assert.IsTrue(cq.GetPrivateArrayCopy().SequenceEqual(new string[] { empty, empty, empty }));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(10000)]
        public void ShouldRunThreadSafeWithLockDenialsOne(int capacity)
        {
            var q = new Queue<int>(capacity);
            var cq = new ConcurrentQueue<int>(q);
            ConcurrentStack<bool> stack = new();
            int result = 0;
            int number = 5;
            Random rnd = new();
            taskList.Clear();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryEnq(number)); } }));
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { Task.Yield(); stack.Push(cq.TryDeq(out result)); } }));
                stack.Push(Task.Run(() => cq.TryEnq(number)).Result);
            }

            taskList.ForEach(t => t.GetAwaiter().GetResult());

            var falls = stack.Any(a => a == false);
            var fallsCount = stack.Count(a => a == false);
            Console.WriteLine($"Faults is present: {falls}");
            Console.WriteLine($"Faults count: {Math.Round((double)fallsCount / stack.Count, 4)}");

            Assert.IsTrue(falls);
            Assert.IsTrue(fallsCount > 0);
            Assert.IsTrue(cq.GetPrivateSizeCopy() <= capacity);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(10000)]
        public void ShouldRunThreadSafeWithLockDenialsTwo(int capacity)
        {
            var q = new Queue<int>(capacity);
            var cq = new ConcurrentQueue<int>(q);
            ConcurrentStack<bool> stack = new();
            int result = 0;
            int number = 5;
            Random rnd = new();
            taskList.Clear();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                Parallel.Invoke(
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { Task.Yield(); stack.Push(cq.TryEnq(number)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryDeq(out result)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryEnq(number)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryDeq(out result)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryEnq(number)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryDeq(out result)); } })),
                () => taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryEnq(number)); } })),
                () => stack.Push(Task.Run(() => cq.TryEnq(number)).Result)
                    );
            }

            taskList.ForEach(t => { if (t != null) t.GetAwaiter().GetResult(); });

            var falls = stack.Any(a => a == false);
            var fallsCount = stack.Count(a => a == false);
            Console.WriteLine($"Faults is present: {falls}");
            Console.WriteLine($"Faults count: {Math.Round((double)fallsCount / stack.Count, 4)}");

            Assert.IsTrue(falls);
            Assert.IsTrue(fallsCount > 0);
            Assert.IsTrue(cq.GetPrivateSizeCopy() <= capacity);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(10000)]
        public void ShouldRemainFunctionalAfterIntensiveLoad(int capacity)
        {
            var q = new Queue<int>(capacity);
            var cq = new ConcurrentQueue<int>(q);
            ConcurrentStack<bool> stack = new();
            Stack<bool> stack2 = new();
            int result = 0;
            int number = 5;
            Random rnd = new();
            taskList.Clear();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 5; i++) { stack.Push(cq.TryEnq(number)); } }));
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 10; i++) { stack.Push(cq.TryDeq(out result)); } }));
                stack.Push(Task.Run(() => cq.TryEnq(number)).Result);
                stack2.Push(Task.Run(() => cq.TryEnq(number)).Result);
            }

            taskList.ForEach(t => t.GetAwaiter().GetResult());

            var falls = stack.Any(a => a == false);
            var fallsCount = stack.Count(a => a == false);
            Console.WriteLine($"Faults is present: {falls}");
            Console.WriteLine($"Faults count: {Math.Round((double)fallsCount / stack.Count, 4)}");

            cq.TryDeq(out result);
            cq.TryDeq(out result);
            cq.TryDeq(out result);
            cq.TryEnq(number);
            cq.TryDeq(out result);

            Assert.IsTrue(falls);
            Assert.IsTrue(stack2.Any(a => a == true));
            Assert.IsTrue(stack2.Any(a => a == false));

            Assert.IsTrue(cq.GetPrivateSizeCopy() <= capacity);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
            Assert.AreEqual(result, number);
        }

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