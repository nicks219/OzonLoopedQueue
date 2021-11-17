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
        private const int CAPACITY = 3;
        private const int BIG_CAPACITY = 20000;
        private const int SMALL_CAPACITY = 1;
        private const int TEST_COUNT = 1000;
        private readonly string str1 = "A";
        private readonly string str2 = "B";
        private readonly string str3 = "C";
        private readonly List<Task> taskList = new();

        [TestMethod]
        public void ShouldThrowException()
        {
            Assert.ThrowsException<NullReferenceException>(() => new ConcurrentQueue<string>(null));
        }

        [TestMethod]

        public void ShouldConcurrentQueueIndependentOfQueue()
        {
            var q = new Queue<string>(SMALL_CAPACITY);
            var cq = new ConcurrentQueue<string>(q);

            cq.TryEnq(str1);
            cq.TryEnq(str2);
            q.Deq(out string result2);
            q = null;
            cq.TryDeq(out string result);

            Assert.AreEqual(result, str1);

            cq.TryDeq(out result);
            Assert.AreEqual(result, default);
        }

        [TestMethod]

        public void ShouldWorkOneElementCapacityQueue()
        {
            var q = new Queue<string>(SMALL_CAPACITY);
            var cq = new ConcurrentQueue<string>(q);
            
            cq.TryEnq(str1);
            cq.TryEnq(str2);
            cq.TryDeq(out string result);
            
            Assert.AreEqual(result, str1);

            cq.TryDeq(out result);
            Assert.AreEqual(result, default);
        }

        [TestMethod]
        public void ShouldEnqueueCorrectly()
        {
            var q = new Queue<string>(CAPACITY);
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
        public void ShouldDequeueCorrectrly()
        {
            var q = new Queue<string>(CAPACITY);
            var cq = new ConcurrentQueue<string>(q);
            string empty = default;

            Assert.IsFalse(cq.TryDeq(out string result));
            Assert.IsFalse(cq.TryDeq(out result));
            Assert.IsFalse(cq.TryDeq(out result));
            Assert.IsFalse(cq.TryDeq(out result));

            Assert.IsTrue(cq.TryEnq(str1));
            Assert.IsTrue(cq.TryEnq(str2));
            Assert.IsTrue(cq.TryEnq(null));
            Assert.IsFalse(cq.TryEnq(str3));

            Assert.IsTrue(cq.TryDeq(out result));
            Assert.AreEqual(result, str1);
            Assert.IsTrue(cq.TryDeq(out result));
            Assert.AreEqual(result, str2);
            Assert.IsTrue(cq.TryDeq(out result));
            Assert.AreEqual(result, null);
            Assert.IsFalse(cq.TryDeq(out result));

            cq.GetPrivateArrayCopy().ToList().ForEach(a => Console.WriteLine(a));
            Assert.IsTrue(cq.GetPrivateArrayCopy().SequenceEqual(new string[] { empty, empty, empty }));
        }

        /// <summary>
        /// Самодельный нагрузочный тест
        /// </summary>
        [TestMethod]
        public void ShouldRunThreadSafeWithFaults()
        {
            var q = new Queue<int>(BIG_CAPACITY);
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

            //Assert.IsTrue(falls);
            //Assert.IsTrue(fallsCount > 0);
            Assert.IsTrue(cq.GetPrivateSizeCopy() <= BIG_CAPACITY);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
        }

        [TestMethod]
        public void ShouldRunThreadSafeOnSmallBufferWithFaults()
        {
            var q = new Queue<int>(SMALL_CAPACITY);
            var cq = new ConcurrentQueue<int>(q);
            ConcurrentStack<bool> stack = new();
            int result = 0;
            int number = 5;
            Random rnd = new();
            taskList.Clear();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryEnq(number)); } }));
                taskList.Add(Task.Run(() => { for (int i = 0; i < TEST_COUNT / 20; i++) { stack.Push(cq.TryDeq(out result)); } }));
                stack.Push(Task.Run(() => cq.TryEnq(number)).Result);
            }

            taskList.ForEach(t => t.GetAwaiter().GetResult());

            var falls = stack.Any(a => a == false);
            var fallsCount = stack.Count(a => a == false);
            Console.WriteLine($"Faults is present: {falls}");
            Console.WriteLine($"Faults count: {Math.Round((double)fallsCount / stack.Count, 4)}");

            //Assert.IsTrue(falls);
            //Assert.IsTrue(fallsCount > 0);
            Assert.IsTrue(cq.GetPrivateSizeCopy() <= BIG_CAPACITY);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
        }

        [TestMethod]
        public void ShouldRemainFunctionalAfterIntensiveLoad()
        {
            var q = new Queue<int>(SMALL_CAPACITY);
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

            //Assert.IsTrue(falls);
            Assert.IsTrue(stack2.Any(a => a == true));
            //Assert.IsTrue(stack2.Any(a => a == false));

            cq.TryDeq(out result);
            cq.TryDeq(out result);
            cq.TryDeq(out result);
            cq.TryEnq(number);
            cq.TryDeq(out result);

            Assert.IsTrue(cq.GetPrivateSizeCopy() <= BIG_CAPACITY);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
            Assert.AreEqual(result, number);
        }

        [TestMethod]
        public void ShouldRunWithParallelInvoke()
        {
            var q = new Queue<int>(BIG_CAPACITY);
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

            taskList.ForEach(t => { if (t.IsCompleted) t.GetAwaiter().GetResult(); });

            var falls = stack.Any(a => a == false);
            var fallsCount = stack.Count(a => a == false);
            Console.WriteLine($"Faults is present: {falls}");
            Console.WriteLine($"Faults count: {Math.Round((double)fallsCount / stack.Count, 4)}");

            //Assert.IsTrue(falls);
            //Assert.IsTrue(fallsCount > 0);
            Assert.IsTrue(cq.GetPrivateSizeCopy() <= BIG_CAPACITY);
            Assert.IsTrue(cq.GetPrivateSizeCopy() >= 0);
        }

        /// <summary>
        /// Самодельный бенчмарк
        /// </summary>
        [TestMethod]
        public void SynchronousBenchmark()
        {
            int testCount = 3000000;
            int bufferSize = 10000;
            var q = new Queue<long>(bufferSize);
            var cq = new ConcurrentQueue<long>(q);
            List<long> list = new(testCount);
            Stopwatch stopWatch = new();
            stopWatch.Start();

            for (int i = 0; i < testCount; i++)
            {
                Parallel.Invoke(
                    () => cq.TryEnq(i),
                    () =>
                    {
                        cq.TryDeq(out long result);
                        list.Add(result);
                    }
                    );
            }

            stopWatch.Stop();
            var time = stopWatch.Elapsed.TotalSeconds;
            int repeats = 0;
            int faults = 0;

            for (int i = 1; i < list.Count; i++)
            {
                if (list[i] == list[i - 1]) repeats++;
                if (list[i] != list[i - 1] + 1) { faults++; i++; }
            }

            Assert.IsTrue(repeats == 0);
            Assert.IsTrue(faults <= 1);

            Console.WriteLine($"Repeats: {(double)repeats / list.Count} ({repeats})");
            Console.WriteLine($"Faults: {(double)faults / list.Count} ({faults})");
            Console.WriteLine($"Requests per second: {Math.Round(testCount / time, 0)}");
        }
    }
}