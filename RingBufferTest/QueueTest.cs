using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RingBufferTest
{
    [TestClass]
    public class QueueTest
    {
        private const int CAPACITY = 3;
        private const int BIG_CAPACITY = 20000;
        private const int SMALL_CAPACITY = 1;
        private const int TEST_COUNT = 10000;
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
            var q = new RingBuffer.Queue<string>(SMALL_CAPACITY);
            q.Enq(str1);
            q.Enq(str2);
            q.Deq(out string result);
            Assert.AreEqual(result, str1);

            q.Deq(out result);
            Assert.AreEqual(result, default(string));
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
        /// Самодельный нагрузочный тест
        /// </summary>
        [TestMethod]
        public void ShouldRingBufferRunThreadSafe()
        {
            var q = new RingBuffer.Queue<int>(BIG_CAPACITY);
            int result = 0;
            int number = 5;
            Random rnd = new();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                int step = rnd.Next(2);
                if (step == 0)
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Enq(number));
                    Task.Run(() => q.Enq(number));
                    Task.Run(() => q.Deq(out result));
                    q.Enq(number);
                }
                else
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Deq(out result));
                    Task.Run(() => q.Enq(number));
                    Task.Run(() => q.Deq(out result));
                    q.Deq(out result);
                }

            }

            Assert.IsTrue(q.GetPrivateSizeCopy() <= BIG_CAPACITY);
            Assert.IsTrue(q.GetPrivateSizeCopy() >= 0);

            // ждем завершения всех Tasks
            Thread.Sleep(50); 

            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);

            q.Enq(number);
            q.Deq(out result);

            Assert.AreEqual(result, number);
        }

        /// <summary>
        /// Самодельный нагрузочный тест
        /// </summary>
        [TestMethod]
        public void ShouldRingBufferRunThreadSafeWithSmallBufferSize()
        {
            var q = new RingBuffer.Queue<int>(SMALL_CAPACITY);
            int result = 0;
            int number = 5;
            Random rnd = new();

            for (int i = 0; i < TEST_COUNT; i++)
            {
                int step = rnd.Next(2);
                if (step == 0)
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Enq(number));
                    Task.Run(() => q.Enq(number));
                    Task.Run(() => q.Deq(out result));
                    q.Enq(number);
                }
                else
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Deq(out result));
                    Task.Run(() => q.Enq(number));
                    Task.Run(() => q.Deq(out result));
                    q.Deq(out result);
                }

            }

            Assert.IsTrue(q.GetPrivateSizeCopy() <= SMALL_CAPACITY);
            Assert.IsTrue(q.GetPrivateSizeCopy() >= 0);

            // ждем завершения всех Tasks
            Thread.Sleep(50); 

            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);

            q.Enq(number);
            q.Deq(out result);

            Assert.AreEqual(result, number);
        }

        /// <summary>
        /// Воспроизведение ситуации, в которой появлялась ошибка
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