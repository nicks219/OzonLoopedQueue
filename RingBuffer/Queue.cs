using System;
using System.Linq;
using System.Threading;

namespace RingBuffer
{
    // Кольцевой буфер. Очередь (FIFO) на массиве фиксированного размера.

    /// <summary>
    /// Неблокирующий потокобезопасный кольцевой буффер
    /// </summary>
    /// <typeparam name="T">Тип хранимых данных</typeparam>
    public class Queue<T>
    {
        private static int _usingResource = 0;
        private readonly T[] _array;
        private readonly int _capacity;
        private int _head = 0;
        private int _tail = 0;
        private int _size = 0;

        public Queue(int capacity)
        {
            if (capacity <= 0) throw new ArgumentException("Capacity must be greater than zero.");

            _array = new T[capacity];
            _capacity = capacity;
        }

        // Добавить элемент в массив
        // `true` если удалось добавить элемент в очередь (ещё осталось место). В противном случае `false`
        public bool Enq(T item)
        {
            bool result = false;

            if (0 == Interlocked.Exchange(ref _usingResource, 1))
            {
                if (_size != _capacity)
                {
                    int previousIndex = _tail;
                    _tail = (++_tail) % _capacity;
                    _array[previousIndex] = item;
                    _size++;
                    result = true;
                }

                Interlocked.Exchange(ref _usingResource, 0);
            }

            return result;
        }

        // Извлечь элемент из массива
        // возвращает `true` если очередь была не пустой и в out-параметре вернется значение первого в очереди элемента.
        // `false` если очередь пуста и возвращать нечего.</returns>
        public bool Deq(out T item)
        {
            item = default;
            bool result = false;

            if (0 == Interlocked.Exchange(ref _usingResource, 1))
            {
                if (_size != 0)
                {
                    item = _array[_head];
                    _array[_head] = default;
                    _head = (++_head) % _capacity;
                    _size--;
                    result = true;
                }

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