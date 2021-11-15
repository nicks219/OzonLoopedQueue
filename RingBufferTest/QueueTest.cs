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
        private const int Capacity = 3;
        private string str1 = "A";
        private string str2 = "B";
        private string str3 = "C";

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
        /// Ќагрузочный тест помог мне найти баг в алгоритме
        /// </summary>
        [TestMethod]
        public void ShouldRingBufferRunsThreadSafeWithLongQueue()
        {
            var q = new RingBuffer.Queue<int>(20000);
            int result = 0;
            int testNumber = 5;
            Random rnd = new Random();

            for (int i = 0; i < 10000; i++)
            {
                int step = rnd.Next(2);
                if (step == 0)
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Enq(testNumber));
                    Task.Run(() => q.Enq(testNumber));
                    Task.Run(() => q.Deq(out result));
                    //Thread.Yield(); // думаю, это бесполезно
                    q.Enq(testNumber);
                }
                else
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Deq(out result));
                    Task.Run(() => q.Enq(testNumber));
                    Task.Run(() => q.Deq(out result));
                    //Thread.Yield(); // думаю, это бесполезно
                    q.Deq(out result);
                }

            }

            Thread.Sleep(50); // иногда все таски не успевают завершитьс€
            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);

            q.Enq(testNumber);
            q.Deq(out result);

            Assert.AreEqual(result, testNumber);
        }

        /// <summary>
        /// Ќагрузочный тест помог мне найти баг в алгоритме
        /// </summary>
        [TestMethod]
        public void ShouldRingBufferRunsThreadSafeWithSmallQueue()
        {
            var q = new RingBuffer.Queue<int>(1);
            int result = 0;
            int testNumber = 5;
            Random rnd = new Random();

            for (int i = 0; i < 10000; i++)
            {
                int step = rnd.Next(2);
                if (step == 0)
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Enq(testNumber));
                    Task.Run(() => q.Enq(testNumber));
                    Task.Run(() => q.Deq(out result));
                    //Thread.Yield(); // думаю, это бесполезно
                    q.Enq(testNumber);
                }
                else
                {
                    Task.Delay(rnd.Next(10)).GetAwaiter().OnCompleted(() => q.Deq(out result));
                    Task.Run(() => q.Enq(testNumber));
                    Task.Run(() => q.Deq(out result));
                    //Thread.Yield(); // думаю, это бесполезно
                    q.Deq(out result);
                }

            }

            Thread.Sleep(50); // иногда все таски не успевают завершитьс€
            q.Deq(out result);
            q.Deq(out result);
            q.Deq(out result);

            q.Enq(testNumber);
            q.Deq(out result);

            Assert.AreEqual(result, testNumber);
        }

        /// <summary>
        /// ¬оспроизведение ситуации, в которой по€вл€лась ошибка
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