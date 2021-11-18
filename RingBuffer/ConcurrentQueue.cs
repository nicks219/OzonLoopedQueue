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
            while (0 != Interlocked.Exchange(ref _usingResource, 1)) { }

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

            int count = 10;

            // Усредненная произовдительность: 1 млн ~ 3 млн запросов в секунду в моём сценарии
            while (0 != Interlocked.Exchange(ref _usingResource, 2))
            {
                if (count-- < 0)
                {
                    Thread.Yield();

                    return false;
                }
            }

            bool result = _queue.Deq(out item);

            Interlocked.Exchange(ref _usingResource, 0);

            return result;
        }

        /// <summary>
        /// Прочитать весь буфер
        /// </summary>
        /// <returns>Последовательность элементов</returns>
        public IEnumerable<T> TryDeqAll()
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