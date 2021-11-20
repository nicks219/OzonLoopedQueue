using System;
using System.Collections.Generic;
using System.Threading;

namespace RingBuffer
{
    /// <summary>
    /// lock-free потокобезопасная обертка над кольцевым буфером
    /// </summary>
    /// <typeparam name="T">Тип хранимых данных</typeparam>
    public class ConcurrentQueue<T>
    {
        private readonly Queue<T> _queue;
        volatile private int _usingResource = 0;

        public ConcurrentQueue(Queue<T> queue)
        {
            if (queue is null) throw new NullReferenceException("Queue must be not null.");

            _queue = new Queue<T>(queue);
        }

        /// <summary>
        /// Попытка записать значение в буфер
        /// </summary>
        /// <param name="item">Записываемое значение</param>
        /// <returns>Удалось или нет записать значение</returns>
        public bool TryEnq(T item)
        {
            //while (0 != Interlocked.Exchange(ref _usingResource, 1)) { }//
            int count = 5;

            while (0 != Interlocked.Exchange(ref _usingResource, 2))
            {
                if (count-- < 0)
                {
                    Thread.Yield();

                    while (0 != Interlocked.Exchange(ref _usingResource, 2))
                    {
                        Thread.Yield();

                        if (count++ > 5)
                        {
                            return false;
                        }
                    }

                    break;
                }
            }

            bool result = _queue.Enq(item);

            Interlocked.Exchange(ref _usingResource, 0);

            return result;

        }

        /// <summary>
        /// Попытка прочитать значение из буфера
        /// </summary>
        /// <param name="item">Читаемое значение</param>
        /// <returns>Удалось или нет прочитать значение</returns>
        public bool TryDeq(out T item)
        {
            item = default;
            
            int count = 5;// 10

            while (0 != Interlocked.Exchange(ref _usingResource, 2))
            {
                if (count-- < 0)
                {
                    Thread.Yield();

                    while (0 != Interlocked.Exchange(ref _usingResource, 2))//2
                    {
                        Thread.Yield();//

                        if (count++ > 5)// 2
                        {
                            return false;
                        }
                    }

                    break;
                }
            }

            bool result = _queue.Deq(out item);

            Interlocked.Exchange(ref _usingResource, 0);

            return result;
        }

        /// <summary>
        /// Лениво прочитать весь буфер
        /// </summary>
        /// <returns>Последовательность элементов</returns>
        public IEnumerable<T> TryDeqAllLazy()
        {
            while (0 != Interlocked.Exchange(ref _usingResource, 3)) { }

            while (_queue.GetPrivateSizeCopy() > 0)
            {
                _queue.Deq(out T item);

                yield return item;
            }

            Interlocked.Exchange(ref _usingResource, 0);
        }

        /// <summary>
        /// Прочитать весь буфер в список
        /// </summary>
        /// <param name="list">Заполняемый список</param>
        public void TryDeqAll(List<T> list)
        {
            int count = 5;// 10

            while (0 != Interlocked.Exchange(ref _usingResource, 2))
            {
                if (count-- < 0)
                {
                    Thread.Yield();

                    while (0 != Interlocked.Exchange(ref _usingResource, 2))
                    {
                        if (count++ > 2)
                        {
                            return;
                        }
                    }

                    break;
                }
            }

            while (_queue.GetPrivateSizeCopy() > 0)
            {
                _queue.Deq(out T item);

                list.Add(item);
            }

            Interlocked.Exchange(ref _usingResource, 0);
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns>Queue в виде копии внутреннего массива</returns>
        public T[] GetPrivateArrayCopy()
        {
            return _queue.GetPrivateArrayCopy();
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns> Внутренне поле _size</returns>
        public int GetPrivateSizeCopy()
        {
            return _queue.GetPrivateSizeCopy();
        }
    }
}