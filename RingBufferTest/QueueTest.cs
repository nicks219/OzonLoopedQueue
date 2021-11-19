using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace RingBufferTest
{
    [TestClass]
    public class QueueTest
    {
        private const int CAPACITY = 3;
        private const int TEST_COUNT = 1000;
        private readonly string str1 = "A";
        private readonly string str2 = "B";
        private readonly string str3 = "C";

        [TestMethod]
        public void ShouldThrowException()
        {
            Assert.ThrowsException<ArgumentException>(() => new RingBuffer.Queue<string>(0));
        }

        [TestMethod]
        public void ShouldWorkOneElementCapacityQueue()
        {
            var q = new RingBuffer.Queue<string>(1);
            q.Enq(str1);
            q.Enq(str2);
            q.Deq(out string result);
            Assert.AreEqual(result, str1);

            q.Deq(out result);
            Assert.AreEqual(result, default);
        }

        [TestMethod]
        public void ShouldEnqueueCorrectrly()
        {
            var q = new RingBuffer.Queue<string>(CAPACITY);

            Assert.IsTrue(q.Enq(str1));
            Assert.IsTrue(q.Enq(str2));
            Assert.IsTrue(q.Enq(str3));
            Assert.IsFalse(q.Enq(str3));
            Assert.IsFalse(q.Enq(str2));
            Assert.IsFalse(q.Enq(str1));

            Assert.IsTrue(q.GetPrivateArrayCopy().SequenceEqual(new string[] { str1, str2, str3 }));
        }

        [TestMethod]
        public void ShouldDequeueCorrectrly()
        {
            var q = new RingBuffer.Queue<string>(CAPACITY);
            string empty = default;

            Assert.IsFalse(q.Deq(out string result));
            Assert.IsFalse(q.Deq(out result));
            Assert.IsFalse(q.Deq(out result));
            Assert.IsFalse(q.Deq(out result));
            
            Assert.IsTrue(q.Enq(str1));
            Assert.IsTrue(q.Enq(str2));
            Assert.IsTrue(q.Enq(null));
            Assert.IsFalse(q.Enq(str3));

            Assert.IsTrue(q.Deq(out result));
            Assert.AreEqual(result, str1);
            Assert.IsTrue(q.Deq(out result));
            Assert.AreEqual(result, str2);
            Assert.IsTrue(q.Deq(out result));
            Assert.AreEqual(result, null);
            Assert.IsFalse(q.Deq(out result));

            q.GetPrivateArrayCopy().ToList().ForEach(a => Console.WriteLine(a));
            Assert.IsTrue(q.GetPrivateArrayCopy().SequenceEqual(new string[] { empty, empty, empty }));
        }

        /// <summary>
        /// Тест помог найти баг в алгоритме
        /// </summary>
        [TestMethod]
        public void RandomCalls()
        {
            var q = new RingBuffer.Queue<int>(CAPACITY);
            
            Random rnd = new();
            int testNumber = rnd.Next(int.MaxValue);
            int result;

            for (int i = 0; i < TEST_COUNT; i++)
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
        /// Воспроизведение ситуации с багом
        /// </summary>
        [TestMethod]
        public void CoolTest()
        {
            var q = new RingBuffer.Queue<string>(CAPACITY);
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