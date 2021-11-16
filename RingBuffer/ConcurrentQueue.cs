using System;
using System.Linq;
using System.Threading;

namespace RingBuffer
{
    // Кольцевой буфер. Очередь (FIFO) на массиве фиксированного размера.

    /// <summary>
    /// Неблокирующая потокобезопасная обертка над кольцевым буфером
    /// </summary>
    /// <typeparam name="T">Тип хранимых данных</typeparam>
    public class ConcurrentQueue<T>
    {
        private readonly Queue<T> _queue;
        private int _usingResource = 0;

        public ConcurrentQueue(Queue<T> queue)
        {
            if (queue is null) throw new NullReferenceException("Queue must be not null.");

            _queue = new Queue<T>(queue);
        }

        /// <summary>
        /// Попытка добавить значение в буфер
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Удалось или нет добавить значение</returns>
        public bool TryEnq(T item)
        {
            bool result = false;

            if (0 == Interlocked.Exchange(ref _usingResource, 1))
            {
                _queue.Enq(item);
                result = true;

                Interlocked.Exchange(ref _usingResource, 0);
            }

            return result;
        }

        /// <summary>
        /// Попытка получить значение из буфера
        /// </summary>
        /// <param name="item"></param>
        /// <returns>Удалось или нет получить значение</returns>
        public bool TryDeq(out T item)
        {
            item = default;
            bool result = false;

            if (0 == Interlocked.Exchange(ref _usingResource, 1))
            {
                _queue.Deq(out item);
                result = true;

                Interlocked.Exchange(ref _usingResource, 0);
            }

            return result;
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns>Queue в виде копии внутреннего массива</returns>
        public T[] GetPrivateArrayCopy()
        {
            //T[] newArray = _array.ToArray();
            //return newArray;
            return _queue.GetPrivateArrayCopy();
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns> Внутренне поле _size</returns>
        public int GetPrivateSizeCopy()
        {
            //return _size;
            return _queue.GetPrivateSizeCopy();
        }
    }
}