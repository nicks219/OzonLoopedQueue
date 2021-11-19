using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        public void ShouldThrowExceptions(Queue<string> queue)
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
    }
}