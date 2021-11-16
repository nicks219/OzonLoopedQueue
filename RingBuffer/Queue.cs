using System;
using System.Linq;

namespace RingBuffer
{
    // Кольцевой буфер. Очередь (FIFO) на массиве фиксированного размера.
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

        // Добавить элемент в массив
        // `true` если удалось добавить элемент в очередь (ещё осталось место). В противном случае `false`
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

        // Извлечь элемент из массива
        // возвращает `true` если очередь была не пустой и в out-параметре вернется значение первого в очереди элемента.
        // `false` если очередь пуста и возвращать нечего.</returns>
        public bool Deq(out T item)
        {
            item = default(T);

            if (_size == 0)
            {
                return false;
            }

            item = _array[_head];
            _array[_head] = default(T);
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
    }
}