using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace RingBufferTest
{
    [TestClass]
    public class QueueTest
    {
        private const int Capacity = 3;
        private string str1 = "A";
        private string str2 = "B";
        private string str3 = "C";

        [TestMethod]
        public void ShouldThrowException()
        {
            Assert.ThrowsException<ArgumentException>(() => new RingBuffer.Queue<string>(0));
        }

        [TestMethod]

        public void ShouldWorkOneElementCapacityQueue()
        {
            var q = new RingBuffer.Queue<string>(1);
            string result = string.Empty;
            q.Enq(str1);
            q.Enq(str2);
            q.Deq(out result);
            Assert.AreEqual(result, str1);

            q.Deq(out result);
            Assert.AreEqual(result, default(string));
        }

        [TestMethod]
        public void ShouldEnqueueCorrectrly()
        {
            var q = new RingBuffer.Queue<string>(Capacity);

            Assert.IsTrue(q.Enq(str1)); // true; A - -
            Assert.IsTrue(q.Enq(str2)); // true; A B -
            Assert.IsTrue(q.Enq(str3)); // true; A B C
            Assert.IsFalse(q.Enq(str3));// false A B C
            Assert.IsFalse(q.Enq(str2));// false A B C
            Assert.IsFalse(q.Enq(str1));// false A B C

            Assert.IsTrue(q.GetQueueCopy().SequenceEqual(new string[] { str1, str2, str3 }));
        }

        [TestMethod]
        public void ShouldDequeueCorrectrly()
        {
            var q = new RingBuffer.Queue<string>(Capacity);
            string result = string.Empty;
            string empty = default(string);

            Assert.IsFalse(q.Deq(out result));
            Assert.IsFalse(q.Deq(out result));
            Assert.IsFalse(q.Deq(out result));
            Assert.IsFalse(q.Deq(out result));

            
            Assert.IsTrue(q.Enq(str1)); // true; A - -
            Assert.IsTrue(q.Enq(str2)); // true; A B -
            Assert.IsTrue(q.Enq(null)); // true: A B x
            Assert.IsFalse(q.Enq(str3)); // false

            Assert.IsTrue(q.Deq(out result)); // true; (A) B x
            Assert.AreEqual(result, str1);
            Assert.IsTrue(q.Deq(out result)); // true; - (B) x
            Assert.AreEqual(result, str2);
            Assert.IsTrue(q.Deq(out result)); // true; - - (x)
            Assert.AreEqual(result, null);
            Assert.IsFalse(q.Deq(out result)); // false; - - -

            q.GetQueueCopy().ToList().ForEach(a => Console.WriteLine(a));
            Assert.IsTrue(q.GetQueueCopy().SequenceEqual(new string[] { empty, empty, empty }));
        }

        /// <summary>
        /// Тест помог мне найти баг в алгоритме
        /// </summary>
        [TestMethod]
        public void RandomCalls()
        {
            var q = new RingBuffer.Queue<int>(Capacity);
            int result = 0;
            int testNumber = 5;
            Random rnd = new Random();

            for (int i = 0; i < 1000; i++)
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
        /// Воспроизведение ситуации, в которой появлялась ошибка
        /// </summary>
        [TestMethod]
        public void CoolTest()
        {
            var q = new RingBuffer.Queue<string>(Capacity);
            string result = string.Empty;
            q.Enq(str1);
            q.Enq(str1);
            q.Enq(str1);
            q.Enq(str1);
            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);
            q.Enq(str1);
            q.Deq(out result);
            Assert.AreEqual(result, str1);
        }
    }
}