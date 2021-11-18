using System;
using System.Linq;

namespace RingBuffer
{
    /// <summary>
    /// Кольцевой буфер. Очередь (FIFO) на массиве фиксированного размера.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Queue<T>
    {
        private readonly T[] _array;
        private readonly int _capacity;
        private int _head = 0;
        private int _tail = 0;
        private int _size = 0;

        public Queue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be more than zero.");

            _array = new T[capacity];
            _capacity = capacity;
        }

        public Queue(Queue<T> queue)
        {
            if (queue is null) throw new NullReferenceException("Queue must be not null.");

            _array = queue._array.ToArray();
            _capacity = queue._capacity;
        }

        /// <summary>
        /// Добавить элемент в массив
        /// </summary>
        /// <param name="item"></param>
        /// <returns>`true` если удалось добавить элемент в очередь (ещё осталось место). В противном случае `false`</returns>
        public bool Enq(T item)
        {
            int previousIndex = _tail;

            if (_size == _capacity)
            {
                return false;
            }

            _tail = (++_tail) % _capacity;
            _array[previousIndex] = item;
            _size++;

            return true;
        }

        /// <summary>
        /// Извлечь элемент из массива
        /// </summary>
        /// <param name="item">значение первого в очереди элемента</param>
        /// <returns>`true` если очередь была не пустой, `false` если очередь пуста и возвращать нечего</returns>
        public bool Deq(out T item)
        {
            item = default;

            if (_size == 0)
            {
                return false;
            }

            item = _array[_head];
            _array[_head] = default;
            _head = (++_head) % _capacity;
            _size--;

            return true;
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns>Queue в виде копии внутреннего массива</returns>
        public T[] GetPrivateArrayCopy()
        {
            T[] newArray = _array.ToArray();
            return newArray;
        }

        /// <summary>
        /// Метод для тестирования
        /// </summary>
        /// <returns> Внутренне поле _size</returns>
        public int GetPrivateSizeCopy()
        {
            return _size;
        }
    }
}