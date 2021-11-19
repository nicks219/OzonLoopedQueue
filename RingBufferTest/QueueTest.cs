using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RingBufferTest
{
    [TestClass]
    public class QueueTest
    {
        private readonly string str1 = "A";

        [TestMethod]
        [DataRow(default)]
        [DataRow(int.MinValue)]
        public void ShouldThrowException(int capacity)
        {
            Assert.ThrowsException<ArgumentException>(() => new RingBuffer.Queue<string>(capacity));
        }

        [TestMethod]
        [DataRow(typeof(int))]
        [DataRow(typeof(string))]
        [DataRow(typeof(QueueTest))]
        public void ShouldCreateDifferentTypes(Type queueGenericType)
        {
            Type type = typeof(RingBuffer.Queue<>).MakeGenericType(queueGenericType.GetType());
            dynamic a_Context = Activator.CreateInstance(type, new object[] { 1 });

            Assert.IsNotNull(type.GetMethod("GetPrivateArrayCopy").Invoke(a_Context, null));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(1000)]
        public void ShouldEnqueAndDequeCorrectly(int capacity)
        {
            RingBuffer.Queue<int> q = new(capacity);

            List<int> testList = Enumerable.Range(1, capacity).ToList();
            List<int> resultList = new();
            testList.ForEach(i => Assert.IsTrue(q.Enq(i)));
            List<int> innerList = q.GetPrivateArrayCopy().ToList();
            testList.ForEach(i => { Assert.IsTrue(q.Deq(out int r)); resultList.Add(r); });

            Assert.IsTrue(innerList.SequenceEqual(testList));
            Assert.IsTrue(resultList.SequenceEqual(testList));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(1000)]
        public void ShouldEnqueueCorrectrly(int capacity)
        {
            RingBuffer.Queue<int> q = new(capacity);

            List<int> testList = Enumerable.Repeat(1, capacity).ToList();
            testList.ForEach(i => Assert.IsTrue(q.Enq(i)));
            Enumerable
                .Repeat(1, capacity)
                .ToList()
                .ForEach(i => Assert.IsFalse(q.Enq(i)));

            Assert.IsTrue(q.GetPrivateArrayCopy().SequenceEqual(testList));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(100)]
        [DataRow(1000)]
        public void ShouldDequeueCorrectrly(int capacity)
        {
            RingBuffer.Queue<int> q = new(capacity);
            List<int> testList = Enumerable.Repeat(1, capacity).ToList();
            testList.ForEach(i => { Assert.IsFalse(q.Deq(out int r)); });

            int number = int.MaxValue;
            q.Enq(number);
            q.Deq(out int result);
            q.Deq(out int result2);

            Assert.AreEqual(result, number);
            Assert.AreEqual(result2, default);
        }

        /// <summary>
        /// Тест помог найти баг в алгоритме, оставлю "как есть"
        /// </summary>
        [TestMethod]
        [DataRow(1, 1000)]
        [DataRow(3, 1000)]
        [DataRow(100, 10000)]
        [DataRow(999, 1000)]
        public void ShouldEnqueAndDequeAdventitiously(int capacity, int testCount)
        {
            RingBuffer.Queue<int> q = new(capacity);

            Random rnd = new();
            int testNumber = rnd.Next(int.MaxValue);
            int result;
            for (int i = 0; i < testCount; i++)
            {
                int step = rnd.Next(2);
                if (step == 0)
                {
                    q.Enq(testNumber);
                }
                else
                {
                    q.Deq(out result);
                }

            }

            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);
            q.Enq(testNumber);
            q.Deq(out result);

            Assert.AreEqual(result, testNumber);
        }

        /// <summary>
        /// Воспроизведение ситуации с багом, оставлю "как есть"
        /// </summary>
        [TestMethod]
        [DataRow(3)]
        public void FixedBug(int capacity)
        {
            RingBuffer.Queue<string> q = new(capacity);

            q.Enq(str1);
            q.Enq(str1);
            q.Enq(str1);
            q.Enq(str1);
            _ = q.Deq(out _);
            _ = q.Deq(out _);
            _ = q.Deq(out _);
            _ = q.Deq(out _);
            q.Enq(str1);
            q.Deq(out string result);

            Assert.AreEqual(result, str1);
        }
    }
}