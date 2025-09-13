using System.Collections;

namespace Vocal;

public class CircularArray<T>(int capacity) : IEnumerable<T>
{
    private readonly T[] _buffer = new T[capacity];
    private int _start;
    private int _count;

    public int Capacity { get; } = capacity;
    public int Count => _count;

    public void Add(T item)
    {
        int index = (_start + _count) % Capacity;
        _buffer[index] = item;

        if (_count == Capacity)
            _start = (_start + 1) % Capacity;
        else
            _count++;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new Enumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public T this[int index] => _buffer[(_start + index) % Capacity];

    private class Enumerator(CircularArray<T> arr) : IEnumerator<T>
    {
        private int _i;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            _i ++;
            return _i < arr._count;
        }

        public void Reset()
        {
            _i = 0;
        }

        T IEnumerator<T>.Current => arr[_i];
        object? IEnumerator.Current => ((IEnumerator<T>) this).Current;
    }
}