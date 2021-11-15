using System.Linq;

namespace CircleQueue
{
    // Кольцевой буфер. Очередь (FIFO) на массиве фиксированного размера.
    public class Queue<T>
    {
        private readonly T[] _array;
        private readonly int _capacity;
        private int _first = 0;
        private int _last = 0;
        private int _count = 0;

        public Queue(int capacity)
        {
            _array = new T[capacity];
            _capacity = capacity;
        }

        // Добавить элемент в массив
        // `true` если удалось добавить элемент в очередь (ещё осталось место). В противном случае `false`
        public bool Enq(T item)
        {
            int previousIndex = _last;

            if (_count == _capacity)
            {
                return false;
            }

            lock (_array)
            {
                _last = (++_last) % _capacity;
                _array[previousIndex] = item;
                _count++;
            }

            return true;
        }

        // Извлечь элемент из массива
        // возвращает `true` если очередь была не пустой и в out-параметре вернется значение первого в очереди элемента.
        // `false` если очередь пуста и возвращать нечего.</returns>
        public bool Deq(out T item)
        {
            item = default(T);
            if (_count == 0)
            {
                return false;
            }

            item = _array[_first];
            _array[_first] = default(T);
            _first = (++_first) % _capacity;
            _count--;

            return true;
        }

        /// <summary>
        /// Метод добавлен исключительно для тестирования
        /// </summary>
        /// <returns>Queue в виде копии внутреннего массива</returns>
        public T[] GetQueueCopy()
        {
            T[] newarray = _array.ToArray();
            return newarray;
        }
    }
}